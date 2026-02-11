using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using Cinema.Infrastructure.Entities;
using Cinema.Web.ViewModels;

namespace Cinema.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            await CleanupExpiredReservations();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userId = user.Id;
            var now = DateTime.Now;

            var tickets = await _context.Tickets
                .Include(t => t.Payment)
                .Include(t => t.Session).ThenInclude(s => s.Movie)
                .Include(t => t.Session).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(t => t.Seat)
                .Where(t => t.Userid == userId)
                .OrderByDescending(t => t.Bookingtime)
                .ToListAsync();

            var vm = new ProfileViewModel
            {
                User = user,

                // Активні: Оплачені, де сеанс ще не закінчився АБО Бронь, яка ще діє
                ActiveTickets = tickets.Where(t =>
                    (t.Status == (short)TicketStatus.Paid && t.Session.Endtime > now) ||
                    (t.Status == (short)TicketStatus.Reserved && now <= t.Bookingtime.AddMinutes(10))
                )
                .Select(t => new ActiveTicketVm
                {
                    TicketId = t.Id,
                    MovieTitle = t.Session.Movie!.Title,
                    CinemaName = t.Session.Hall!.Cinema!.Name,
                    CinemaCity = t.Session.Hall.Cinema.City,
                    HallName = t.Session.Hall.Name,
                    StartTime = t.Session.Starttime,
                    EndTime = t.Session.Endtime,
                    Row = t.Seat!.Rownumber ?? 0,
                    Seat = t.Seat.Seatnumber ?? 0,
                    Price = t.Price,
                    IsReserved = t.Status == (short)TicketStatus.Reserved,
                    CanReturn = t.Status == (short)TicketStatus.Paid && t.Session.Starttime > now,
                    BookingTime = t.Bookingtime
                })
                .ToList(),


                // Історія: Скасовані АБО Оплачені, де сеанс вже завершився
                HistoryTickets = tickets.Where(t =>
                    t.Status == (short)TicketStatus.Cancelled ||
                    (t.Status == (short)TicketStatus.Paid && t.Session.Endtime <= now)
                )
                .Select(t =>
                {
                    string actionText;
                    string badgeClass;
                    DateTime actionTime;

                    if (t.Status == (short)TicketStatus.Cancelled)
                    {
                        if (t.Payment != null)
                        {
                            actionText = "🔄 Квиток повернено";
                            badgeClass = "bg-warning-subtle text-warning";
                        }
                        else
                        {
                            actionText = "❌ Бронювання скасовано";
                            badgeClass = "bg-danger-subtle text-danger";
                        }

                        actionTime = t.Bookingtime;
                    }
                    else
                    {
                        actionText = "🎬 Сеанс завершився";
                        badgeClass = "bg-secondary-subtle text-secondary";
                        actionTime = t.Session.Endtime;
                    }

                    return new HistoryTicketVm
                    {
                        TicketId = t.Id,
                        MovieTitle = t.Session.Movie!.Title,
                        CinemaName = t.Session.Hall!.Cinema!.Name,
                        CinemaCity = t.Session.Hall.Cinema.City,
                        HallName = t.Session.Hall.Name,
                        StartTime = t.Session.Starttime,
                        Row = t.Seat!.Rownumber ?? 0,
                        Seat = t.Seat.Seatnumber ?? 0,
                        Price = t.Price,
                        ActionText = actionText,
                        BadgeClass = badgeClass,
                        ActionTime = actionTime
                    };
                })
                .ToList()

            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FirstName = model.User.FirstName;
            user.LastName = model.User.LastName;   // нове поле
            user.Email = model.User.Email;
            user.PhoneNumber = model.User.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Не вдалося оновити профіль.");
                return View("Index", model);
            }

            // Оновлюємо claims
            var claims = await _userManager.GetClaimsAsync(user);

            var firstNameClaim = claims.FirstOrDefault(c => c.Type == "FirstName");
            if (firstNameClaim != null)
                await _userManager.RemoveClaimAsync(user, firstNameClaim);
            if (!string.IsNullOrEmpty(user.FirstName))
                await _userManager.AddClaimAsync(user, new Claim("FirstName", user.FirstName));

            var lastNameClaim = claims.FirstOrDefault(c => c.Type == "LastName");
            if (lastNameClaim != null)
                await _userManager.RemoveClaimAsync(user, lastNameClaim);
            if (!string.IsNullOrEmpty(user.LastName))
                await _userManager.AddClaimAsync(user, new Claim("LastName", user.LastName));

            await _signInManager.RefreshSignInAsync(user);

            TempData["ProfileUpdated"] = true;
            return RedirectToAction("Index");
        }

        // Новий метод для повернення квитка
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnTicket(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (!int.TryParse(userId, out var uid))
                return RedirectToAction("Login", "Account");

            var ticket = await _context.Tickets
                .Include(t => t.Session)
                .FirstOrDefaultAsync(t => t.Id == id && t.Userid == uid);

            if (ticket == null)
                return NotFound();

            // Повернути можна тільки оплачений квиток
            if (ticket.Status != (short)TicketStatus.Paid)
            {
                TempData["Error"] = "Можна повернути лише оплачений квиток.";
                return RedirectToAction("Index");
            }

            // ❗ НОВА ПЕРЕВІРКА — якщо сеанс вже почався
            if (DateTime.Now >= ticket.Session.Starttime)
            {
                TempData["Error"] = "Сеанс вже розпочався. Повернення неможливе.";
                return RedirectToAction("Index");
            }


            ticket.Status = (short)TicketStatus.Cancelled;
            ticket.Bookingtime = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["TicketReturned"] = true;
            return RedirectToAction("Index");
        }


        private async Task CleanupExpiredReservations()
        {
            var now = DateTime.Now;

            var expiredTickets = await _context.Tickets
                .Where(t =>
                    t.Status == (short)TicketStatus.Reserved &&
                    now > t.Bookingtime.AddMinutes(10))
                .ToListAsync();

            if (!expiredTickets.Any())
                return;

            foreach (var ticket in expiredTickets)
            {
                ticket.Status = (short)TicketStatus.Cancelled;
                ticket.Bookingtime = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

    }
}
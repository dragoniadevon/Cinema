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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewBag.SelectedCity = Request.Cookies["selectedCity"] ?? "Оберіть місто";
            ViewBag.SelectedCinemaId = Request.Cookies["selectedCinemaId"];

            var userIdString = _userManager.GetUserId(User);
            if (!int.TryParse(userIdString, out var userId))
                return RedirectToAction("Login", "Account");

            var tickets = await _context.Tickets
                .Include(t => t.Session).ThenInclude(s => s.Movie)
                .Include(t => t.Session).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
                .Include(t => t.Seat)
                .Where(t => t.Userid == userId)
                .OrderByDescending(t => t.Session!.Starttime)
                .ToListAsync();

            var vm = new ProfileViewModel
            {
                User = user,
                Tickets = tickets
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            await CleanupExpiredReservations();

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
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ticket = await _context.Tickets
                .Include(t => t.Session)
                .FirstOrDefaultAsync(t => t.Id == id && t.Userid == user.Id);

            if (ticket == null)
                return NotFound();

            // ✅ можна повертати тільки оплачені квитки
            if (ticket.Status != (short)TicketStatus.Paid)
            {
                TempData["Error"] = "Цей квиток не можна повернути.";
                return RedirectToAction("Index");
            }

            // ⏰ не можна після початку сеансу
            if (ticket.Session.Starttime <= DateTime.Now)
            {
                TempData["Error"] = "Сеанс уже почався. Повернення неможливе.";
                return RedirectToAction("Index");
            }

            ticket.Status = (short)TicketStatus.Cancelled;
            ticket.IsReturned = true; // можна залишити, але тільки як “історію”

            await _context.SaveChangesAsync();

            TempData["TicketReturned"] = true;
            return RedirectToAction("Index");
        }


        private async Task CleanupExpiredReservations()
        {
            var now = DateTime.UtcNow;

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
            }

            await _context.SaveChangesAsync();
        }

    }
}

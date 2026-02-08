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
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.IsReturned = true; // позначаємо як повернений
            await _context.SaveChangesAsync();

            TempData["TicketReturned"] = true;
            return RedirectToAction("Index");
        }
    }
}

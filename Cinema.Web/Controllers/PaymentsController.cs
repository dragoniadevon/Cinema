using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Cinema.Web.Models.Payments;

namespace Cinema.Web.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PaymentsController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize]
    public async Task<IActionResult> Pay(string ticketIds)
    {
        if (string.IsNullOrWhiteSpace(ticketIds))
            return RedirectToAction("Index", "Profile");

        var ids = ticketIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToList();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var tickets = await _db.Tickets
            .Include(t => t.Seat)
            .Include(t => t.Session)
                .ThenInclude(s => s.Movie)
            .Where(t =>
                ids.Contains(t.Id) &&
                t.Userid == user.Id &&
                t.Status == (short)TicketStatus.Reserved)
            .ToListAsync();

        if (!tickets.Any())
            return RedirectToAction("Index", "Profile");

        // ⏱ Перевірка таймера (10 хв)
        var now = DateTime.UtcNow;
        var expired = tickets.Any(t =>
            now > t.Bookingtime.AddMinutes(10));

        if (expired)
        {
            TempData["Error"] = "Час бронювання минув.";
            return RedirectToAction("Index", "Profile");
        }

        var vm = new PaymentVm
        {
            Tickets = tickets.Select(t => new TicketPaymentVm
            {
                TicketId = t.Id,
                MovieTitle = t.Session.Movie.Title,
                Row = t.Seat.Rownumber ?? 0,
                SeatNumber = t.Seat.Seatnumber ?? 0,
                Price = t.Price
            }).ToList(),

            TotalAmount = tickets.Sum(t => t.Price),
            MinutesLeft = 10 - (int)(now - tickets.Min(t => t.Bookingtime)).TotalMinutes
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(List<int> ticketIds)
    {
        if (ticketIds == null || ticketIds.Count == 0)
            return RedirectToAction("Index", "Profile");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var tickets = await _db.Tickets
            .Include(t => t.Payment)
            .Where(t =>
                ticketIds.Contains(t.Id) &&
                t.Userid == user.Id &&
                t.Status == (short)TicketStatus.Reserved)
            .ToListAsync();

        if (!tickets.Any())
            return RedirectToAction("Index", "Profile");

        var now = DateTime.UtcNow;

        foreach (var ticket in tickets)
        {
            if (now > ticket.Bookingtime.AddMinutes(10))
            {
                TempData["Error"] = "Час бронювання минув.";
                return RedirectToAction("Index", "Profile");
            }

            var payment = new Payment
            {
                Ticketid = ticket.Id,
                Amount = ticket.Price,
                Paymentdate = now,
                Status = (short)PaymentStatus.Paid
            };

            ticket.Status = (short)TicketStatus.Paid;

            _db.Payments.Add(payment);
        }

        await _db.SaveChangesAsync();

        return RedirectToAction(
            "OrderConfirmation",
            "Tickets",
            new { ticketIds = string.Join(",", ticketIds) }
        );
    }

}
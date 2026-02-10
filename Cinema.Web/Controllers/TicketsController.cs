using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Cinema.Infrastructure.Entities;
using Cinema.Web.Models.Tickets;


namespace Cinema.Web.Controllers;

public class TicketsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;


    public TicketsController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(ReserveTicketsRequest request)
    {
        if (!ModelState.IsValid || request.SeatIds.Count == 0)
        {
            TempData["Error"] = "Не обрано жодного місця.";
            return RedirectToAction("Details", "Sessions", new { id = request.SessionId });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var session = await _db.Sessions
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId);

        if (session == null)
            return NotFound();

        var categoryId = session.Hall?.Halltype == 2 ? 2 : 1;

        var sessionPrice = await _db.Sessionprices.FirstOrDefaultAsync(sp =>
            sp.Sessionid == request.SessionId &&
            sp.Categoryid == categoryId);

        if (sessionPrice == null)
        {
            TempData["Error"] = "Для цього сеансу не задано ціну.";
            return RedirectToAction("Details", "Sessions", new { id = request.SessionId });
        }

        var now = DateTime.UtcNow;

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 🔒 1. Перевіряємо ВСІ місця ОДНИМ запитом
            var busySeatIds = await _db.Tickets
                .Where(t =>
                    t.Sessionid == request.SessionId &&
                    request.SeatIds.Contains(t.Seatid) &&
                    (
                        t.Status == (short)TicketStatus.Paid ||
                        (t.Status == (short)TicketStatus.Reserved &&
                         now <= t.Bookingtime.AddMinutes(10))
                    )
                )
                .Select(t => t.Seatid)
                .Distinct()
                .ToListAsync();

            if (busySeatIds.Any())
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Деякі місця вже зайняті.";
                return RedirectToAction("Details", "Sessions", new { id = request.SessionId });
            }

            // 🆕 2. Створюємо квитки
            foreach (var seatId in request.SeatIds)
            {
                _db.Tickets.Add(new Ticket
                {
                    Userid = user.Id,
                    Sessionid = request.SessionId,
                    Seatid = seatId,
                    Price = sessionPrice.Price,
                    Status = (short)TicketStatus.Reserved,
                    Bookingtime = now
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Не вдалося забронювати місця. Спробуйте ще раз.";
            return RedirectToAction("Details", "Sessions", new { id = request.SessionId });
        }

        var ticketIds = await _db.Tickets
            .Where(t =>
                t.Userid == user.Id &&
                t.Sessionid == request.SessionId &&
                t.Status == (short)TicketStatus.Reserved &&
                request.SeatIds.Contains(t.Seatid))
            .Select(t => t.Id)
            .ToListAsync();

        return RedirectToAction(
            "Pay",
            "Payments",
            new { ticketIds = string.Join(",", ticketIds) }
        );
    }

    [Authorize]
    public async Task<IActionResult> OrderConfirmation(string ticketIds)
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
                t.Status == (short)TicketStatus.Paid)
            .ToListAsync();

        if (!tickets.Any())
            return RedirectToAction("Index", "Profile");

        var vm = new OrderConfirmationVm
        {
            Tickets = tickets.Select(t => new TicketConfirmationItemVm
            {
                TicketId = t.Id,
                MovieTitle = t.Session.Movie.Title,
                Row = t.Seat.Rownumber ?? 0,
                Seat = t.Seat.Seatnumber ?? 0,
                Price = t.Price
            }).ToList()
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelReservation(int ticketId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId
                                   && t.Userid == user.Id
                                   && t.Status == (short)TicketStatus.Reserved);

        if (ticket == null)
        {
            TempData["Error"] = "Бронювання не знайдено або вже недійсне.";
            return RedirectToAction("Index", "Profile");
        }

        ticket.Status = (short)TicketStatus.Cancelled;
        // Обов'язково скидаємо час бронювання або залишаємо для історії
        await _db.SaveChangesAsync();

        TempData["Info"] = "Бронювання успішно скасовано. Місце знову вільне.";
        return RedirectToAction("Index", "Profile");
    }
}
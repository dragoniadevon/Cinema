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


    // GET: /Tickets/Book?sessionId=1
    [Authorize]
    public async Task<IActionResult> Book(int sessionId)
    {
        var session = await _db.Sessions
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return NotFound();

        var seats = await _db.Seats
            .Where(x => x.Hallid == session.Hallid)
            .OrderBy(x => x.Rownumber)
            .ThenBy(x => x.Seatnumber)
            .ToListAsync();

        var takenSeatIds = await _db.Tickets
            .Where(t => t.Sessionid == sessionId)
            .Select(t => t.Seatid)
            .ToListAsync();


        var vm = new BookTicketVm
        {
            SessionId = sessionId,
            Seats = seats.Select(s => new SeatVm
            {
                SeatId = s.Id,
                Row = s.Rownumber ?? 0,
                Number = s.Seatnumber ?? 0,
                IsTaken = takenSeatIds.Contains(s.Id)
            }).ToList()
        };

        return View(vm);
    }

    // POST: /Tickets/Book
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookTicketRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        int userId = user.Id;


        // 1) Достаём сеанс + зал
        var session = await _db.Sessions
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId);

        if (session == null)
            return NotFound();

        // 2) Определяем категорию цены (1=Standard, 2=VIP)
        var categoryId = session.Hall?.Halltype == 2 ? 2 : 1;

        // 3) Берём цену из Sessionprices
        var sessionPrice = await _db.Sessionprices
            .FirstOrDefaultAsync(sp =>
                sp.Sessionid == request.SessionId &&
                sp.Categoryid == categoryId);

        if (sessionPrice == null)
        {
            TempData["Error"] = "Для цього сеансу не задана ціна 😿 (таблиця Sessionprices порожня).";
            return RedirectToAction(nameof(Book), new { sessionId = request.SessionId });
        }

        decimal price = sessionPrice.Price;

        var ticket = new Ticket
        {
            Userid = userId,
            Sessionid = request.SessionId,
            Seatid = request.SeatId,
            Price = price,
            Status = 1, // Reserved
            Bookingtime = DateTime.UtcNow
        };

        _db.Tickets.Add(ticket);

        try
        {
            await _db.SaveChangesAsync(); // UNIQUE(SessionId, SeatId) ловит двойную покупку
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Це місце щойно зайняли 😿 Обери інше.";
            return RedirectToAction(nameof(Book), new { sessionId = request.SessionId });
        }

        return RedirectToAction(nameof(Confirmation), new { id = ticket.Id });
    }

    // GET: /Tickets/Confirmation?id=123
    public async Task<IActionResult> Confirmation(int id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Seat)
            .Include(t => t.Session)
                .ThenInclude(s => s.Movie)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
            return NotFound();

        return View(ticket);
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

        var sessionPrice = await _db.Sessionprices
            .FirstOrDefaultAsync(sp =>
                sp.Sessionid == request.SessionId &&
                sp.Categoryid == categoryId);

        if (sessionPrice == null)
        {
            TempData["Error"] = "Для цього сеансу не задано ціну.";
            return RedirectToAction("Details", "Sessions", new { id = request.SessionId });
        }

        foreach (var seatId in request.SeatIds)
        {
            var ticket = new Ticket
            {
                Userid = user.Id,
                Sessionid = request.SessionId,
                Seatid = seatId,
                Price = sessionPrice.Price,
                Status = (short)TicketStatus.Reserved,
                Bookingtime = DateTime.UtcNow,
                IsReturned = false
            };

            _db.Tickets.Add(ticket);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Деякі місця вже були заброньовані іншими користувачами.";
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
        if (user == null)
            return Challenge();

        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(t =>
                t.Id == ticketId &&
                t.Userid == user.Id &&
                t.Status == (short)TicketStatus.Reserved);

        if (ticket == null)
            return RedirectToAction("Index", "Profile");

        ticket.Status = (short)TicketStatus.Cancelled;

        await _db.SaveChangesAsync();

        TempData["Info"] = "Бронювання скасовано.";

        return RedirectToAction("Index", "Profile");
    }
}
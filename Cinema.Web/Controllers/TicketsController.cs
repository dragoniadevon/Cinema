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
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;

namespace Cinema.Web.Controllers;

public class TicketsController : Controller
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Tickets/Book?sessionId=1
    public async Task<IActionResult> Book(int sessionId)
    {
        var session = await _db.Sessions
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return NotFound();

        var seats = await _db.Seats
            .Where(x => x.Hallid == session.Hallid)
            .OrderBy(x => x.Rownumber)
            .ThenBy(x => x.Seatnumber)
            .ToListAsync();

        var takenSeatIds = await _db.Tickets
            .Where(t => t.Sessionid == sessionId)
            .Select(t => t.Seatid!.Value)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookTicketRequest request)
    {
        int? userId = null;
        decimal price = 200m;

        var ticket = new Ticket
        {
            Userid = userId,
            Sessionid = request.SessionId,
            Seatid = request.SeatId,
            Price = price,
            Status = 1,
            Bookingtime = DateTime.UtcNow
        };

        _db.Tickets.Add(ticket);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Це місце щойно зайняли 😿 Обери інше.";
            return RedirectToAction(nameof(Book), new { sessionId = request.SessionId });
        }

        return RedirectToAction(nameof(Confirmation), new { id = ticket.Id });
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Seat)
            .Include(t => t.Session)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        return View(ticket);
    }
}

public class BookTicketRequest
{
    public int SessionId { get; set; }
    public int SeatId { get; set; }
}

public class BookTicketVm
{
    public int SessionId { get; set; }
    public List<SeatVm> Seats { get; set; } = new();
}

public class SeatVm
{
    public int SeatId { get; set; }
    public int Row { get; set; }
    public int Number { get; set; }
    public bool IsTaken { get; set; }
}
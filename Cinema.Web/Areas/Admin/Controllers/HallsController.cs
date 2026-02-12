using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Infrastructure.Entities.Enums;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Cinema.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HallsController : Controller
{
    private readonly AppDbContext _context;

    public HallsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Create(int cinemaId)
    {
        ViewBag.CinemaId = cinemaId;
        return View();
    }

    public async Task<IActionResult> Details(int id)
    {
        var hall = await _context.Halls
            .Include(h => h.Seats)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hall == null)
        {
            return NotFound();
        }

        return View(hall);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Hall hall, short? vipSeats)
    {
        if (await _context.Halls.AnyAsync(h => h.Name.ToLower() == hall.Name.ToLower() && h.Cinemaid == hall.Cinemaid))
        {
            ModelState.AddModelError("Name", "Зал з такою назвою вже існує в цьому кінотеатрі.");
        }

        if (hall.Halltype == (short)HallType.Mixed)
        {
            if (!vipSeats.HasValue || vipSeats <= 0)
            {
                ModelState.AddModelError("vipSeats", "Будь ласка, вкажіть кількість VIP-місць для змішаного залу.");
            }
            else if (vipSeats > hall.Seatsperrow)
            {
                ModelState.AddModelError("vipSeats", "VIP-місць не може бути більше, ніж звичайних.");
            }
        }

        if (ModelState.IsValid)
        {
            _context.Halls.Add(hall);
            await _context.SaveChangesAsync();

            short seatsForLastRow = (short)(vipSeats ?? 0);
            var newSeats = GenerateSeatsList(hall, seatsForLastRow);

            _context.Seats.AddRange(newSeats);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Cinemas");
        }

        ViewBag.CinemaId = hall.Cinemaid;
        return View(hall);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var hall = await _context.Halls.Include(h => h.Seats).FirstOrDefaultAsync(h => h.Id == id);
        if (hall == null) return NotFound();

        ViewBag.HasSessions = await _context.Sessions.AnyAsync(s => s.Hallid == id);

        return View(hall);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Hall hall, short? vipSeats)
    {
        if (id != hall.Id) return NotFound();

        var dbHall = await _context.Halls.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
        if (dbHall == null) return NotFound();

        bool nameExists = await _context.Halls.AnyAsync(h =>
            h.Id != id && h.Name.ToLower() == hall.Name.ToLower() && h.Cinemaid == hall.Cinemaid);
        if (nameExists) ModelState.AddModelError("Name", "Назва вже зайнята.");

        bool structureChanged = dbHall.Rows != hall.Rows ||
                                dbHall.Seatsperrow != hall.Seatsperrow ||
                                dbHall.Halltype != hall.Halltype;

        if (structureChanged && await _context.Sessions.AnyAsync(s => s.Hallid == id))
        {
            ModelState.AddModelError("", "Неможливо змінити структуру або тип залу: для нього вже створено сеанси в розкладі.");
        }

        if (hall.Halltype == (short)HallType.Mixed)
        {
            if (!vipSeats.HasValue || vipSeats <= 0)
            {
                ModelState.AddModelError("vipSeats", "Введіть кількість VIP-місць для змішаного залу.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(hall);
                await _context.SaveChangesAsync();

                if (structureChanged || (hall.Halltype == (short)HallType.Mixed && vipSeats.HasValue))
                {
                    var oldSeats = _context.Seats.Where(s => s.Hallid == hall.Id);
                    _context.Seats.RemoveRange(oldSeats);

                    short seatsForMixedRow = vipSeats ?? 0;
                    var newSeats = GenerateSeatsList(hall, seatsForMixedRow);

                    _context.Seats.AddRange(newSeats);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Зал оновлено!";
                return RedirectToAction("Index", "Cinemas");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Halls.Any(e => e.Id == hall.Id)) return NotFound();
                else throw;
            }
        }

        return View(hall);
    }

    private List<Seat> GenerateSeatsList(Hall hall, short vipSeats)
    {
        var seats = new List<Seat>();
        for (short r = 1; r <= hall.Rows; r++)
        {
            bool isFullVipHall = (hall.Halltype == (short)HallType.VIP);
            bool isMixedLastRow = (hall.Halltype == (short)HallType.Mixed && r == hall.Rows);

            int count = isMixedLastRow ? vipSeats : (hall.Seatsperrow ?? 0);

            int categoryId = (isFullVipHall || isMixedLastRow)
                ? (int)SeatCategory.VIP
                : (int)SeatCategory.Standard;

            for (short s = 1; s <= count; s++)
            {
                seats.Add(new Seat
                {
                    Hallid = hall.Id,
                    Rownumber = r,
                    Seatnumber = s,
                    Pricecategoryid = categoryId
                });
            }
        }
        return seats;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveHall(int id)
    {
        var hall = await _context.Halls
            .Include(h => h.Sessions)
                .ThenInclude(s => s.Tickets)
            .Include(h => h.Sessions)
                .ThenInclude(s => s.Sessionprices)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hall == null) return NotFound();

        hall.Isactive = !hall.Isactive;

        if (!hall.Isactive)
        {
            var futureSessions = hall.Sessions.Where(s => s.Starttime >= DateTime.Now).ToList();

            foreach (var session in futureSessions)
            {
                if (session.Tickets.Any())
                {
                    session.Isactive = false;
                    foreach (var ticket in session.Tickets.Where(t => t.Status != (short)TicketStatus.Cancelled))
                    {
                        ticket.Status = (short)TicketStatus.Cancelled;
                        ticket.Bookingtime = DateTime.Now;
                    }
                }
                else
                {
                    _context.Sessionprices.RemoveRange(session.Sessionprices);
                    _context.Sessions.Remove(session);
                }
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = hall.Isactive ? "Зал відновлено!" : "Зал архівовано. Оплачені квитки повернуто клієнтам.";
        return RedirectToAction("Index", "Cinemas");
    }
}
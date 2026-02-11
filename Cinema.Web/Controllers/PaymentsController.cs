using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using Cinema.Infrastructure.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Cinema.Web.Models.Payments;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        if (user == null) return Challenge();

        var tickets = await _db.Tickets
            .Include(t => t.Seat)
            .Include(t => t.Session).ThenInclude(s => s.Movie)
            .Where(t =>
                ids.Contains(t.Id) &&
                t.Userid == user.Id &&
                t.Status == (short)TicketStatus.Reserved
            )
            .ToListAsync();

        if (!tickets.Any())
            return RedirectToAction("Index", "Profile");

        // 1. ПЕРЕВІРКА НА ЗАСТАРІЛУ БРОНЬ
        var now = DateTime.Now;
        var expired = tickets.Any(t => t.Status == (short)TicketStatus.Reserved && now > t.Bookingtime.AddMinutes(10));

        if (expired)
        {
            // Автоматично звільняємо місця
            foreach (var t in tickets.Where(x => x.Status == (short)TicketStatus.Reserved))
            {
                t.Status = (short)TicketStatus.Cancelled;
            }
            await _db.SaveChangesAsync();

            TempData["Error"] = "Час бронювання минув. Будь ласка, оберіть місця знову.";
            return RedirectToAction("Details", "Sessions", new { id = tickets.First().Sessionid });
        }

        // 2. ПЕРЕВІРКА, ЧИ НЕ ОПЛАЧЕНО ВЖЕ
        if (tickets.All(t => t.Status == (short)TicketStatus.Paid))
        {
            return RedirectToAction("OrderConfirmation", "Tickets", new { ticketIds = ticketIds });
        }

        var minBookingTime = tickets.Any() ? tickets.Min(t => t.Bookingtime) : DateTime.Now;

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
            MinutesLeft = Math.Max(0, 10 - (int)(now - minBookingTime).TotalMinutes)
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
        if (user == null) return Challenge();

        // Використовуємо транзакцію для безпеки даних
        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                var tickets = await _db.Tickets
                    .Where(t => ticketIds.Contains(t.Id) && t.Userid == user.Id && t.Status == (short)TicketStatus.Reserved)
                    .ToListAsync();

                if (!tickets.Any())
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Час бронювання минув або квиток більше недоступний.";
                    return RedirectToAction("Index", "Profile");
                }

                var now = DateTime.Now;

                foreach (var ticket in tickets)
                {
                    // Подвійна перевірка часу прямо перед записом в БД
                    if (now > ticket.Bookingtime.AddMinutes(10))
                    {
                        throw new Exception("Timeout");
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
                await transaction.CommitAsync();

                return RedirectToAction("OrderConfirmation", "Tickets", new { ticketIds = string.Join(",", ticketIds) });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Помилка при оплаті або час бронювання вичерпано.";
                return RedirectToAction("Index", "Profile");
            }
        }
    }
}
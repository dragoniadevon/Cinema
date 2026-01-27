using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;

namespace Cinema.Web.Controllers;

public class PaymentsController : Controller
{
    private readonly AppDbContext _db;

    public PaymentsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Payments/Pay?ticketId=5
    public async Task<IActionResult> Pay(int ticketId)
    {
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return NotFound();

        return View(ticket);
    }

    // POST: /Payments/PayConfirm
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayConfirm(int ticketId)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Payment)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return NotFound();

        // если уже оплачено
        if (ticket.Payment != null)
        {
            TempData["Info"] = "Цей квиток вже оплачено.";
            return RedirectToAction("Confirmation", "Tickets", new { id = ticketId });
        }

        var payment = new Payment
        {
            Ticketid = ticketId,
            Amount = ticket.Price,
            Paymentdate = DateTime.UtcNow,
            Status = 1 // 1 = Paid
        };

        ticket.Status = 2; // 2 = Paid

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return RedirectToAction("Confirmation", "Tickets", new { id = ticketId });
    }
}
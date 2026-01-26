using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;

public class ActorsController : Controller
{
    private readonly AppDbContext _context;

    public ActorsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var actors = await _context.Actors.ToListAsync();
        return View(actors);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Actor actor)
    {
        if (actor.Fullname != null)
        {
            actor.Fullname = actor.Fullname.Trim();
        }

        if (string.IsNullOrWhiteSpace(actor.Fullname))
        {
            ModelState.AddModelError(nameof(actor.Fullname), "Імʼя актора обовʼязкове");
            return View(actor);
        }

        try
        {
            _context.Actors.Add(actor);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(actor.Fullname), "Такий актор вже існує");
            return View(actor);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var actor = await _context.Actors.FindAsync(id);
        if (actor == null)
            return NotFound();

        return View(actor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Actor actor)
    {
        if (id != actor.Id)
            return BadRequest();

        if (actor.Fullname != null)
        {
            actor.Fullname = actor.Fullname.Trim();
        }

        if (string.IsNullOrWhiteSpace(actor.Fullname))
        {
            ModelState.AddModelError(nameof(actor.Fullname), "Імʼя актора обовʼязкове");
            return View(actor);
        }

        try
        {
            _context.Update(actor);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(actor.Fullname), "Такий актор вже існує");
            return View(actor);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var actor = await _context.Actors.FindAsync(id);
        if (actor == null)
            return NotFound();

        _context.Actors.Remove(actor);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}

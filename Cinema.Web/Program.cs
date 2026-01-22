using Cinema.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Налаштування сервісів
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// SEED DATA (Halls & Seats)
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var context = services.GetRequiredService<AppDbContext>();

//    // Якщо в базі ще немає жодного кінотеатру — додаємо дані
//    if (!context.Cinemas.Any())
//    {
//        // 1. Створюємо кінотеатр
//        var cinema = new Cinema.Infrastructure.Entities.Cinema
//        {
//            Name = "Планета Кіно",
//            City = "Луцьк",
//            Address = "вул. Соборності, 11"
//        };
//        context.Cinemas.Add(cinema);
//        context.SaveChanges();

//        // 2. Список ваших залів
//        var halls = new[]
//        {
//            new { Name = "Зал №1 (Основний)", Type = (short)1, Rows = (short)10, Seats = (short)15 },
//            new { Name = "Зал №2 (Стандарт)", Type = (short)1, Rows = (short)8, Seats = (short)10 },
//            new { Name = "Зал №3 (VIP)", Type = (short)2, Rows = (short)4, Seats = (short)6 }
//        };

//        foreach (var h in halls)
//        {
//            var hall = new Hall
//            {
//                Name = h.Name,
//                Cinemaid = cinema.Id,
//                Halltype = h.Type,
//                Rows = h.Rows,
//                Seatsperrow = h.Seats
//            };
//            context.Halls.Add(hall);
//            context.SaveChanges();

//            // 3. Генерація місць для кожного залу
//            var seatsList = new List<Seat>();
//            for (short r = 1; r <= hall.Rows; r++)
//            {
//                for (short s = 1; s <= hall.Seatsperrow; s++)
//                {
//                    seatsList.Add(new Seat
//                    {
//                        Hallid = hall.Id,
//                        Rownumber = r,
//                        Seatnumber = s
//                    });
//                }
//            }
//            context.Seats.AddRange(seatsList);
//            context.SaveChanges();
//        }
//    }
//}
// END SEED

// Налаштування конвеєра запитів (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
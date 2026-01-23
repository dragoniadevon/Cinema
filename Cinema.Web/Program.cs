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

// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//     db.Database.ExecuteSqlRaw("""
//         -- ЖАНРИ
//         INSERT INTO Genres (Name)
//         SELECT N'Драма'
//         WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Драма');

//         INSERT INTO Genres (Name)
//         SELECT N'Комедія'
//         WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Комедія');
//         INSERT INTO Genres (Name)
//         SELECT N'Фантастика'
//         WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Фантастика');

//         INSERT INTO Genres (Name)
//         SELECT N'Трилер'
//         WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Трилер');
//         INSERT INTO Genres (Name)
//         SELECT N'Романтика'
//         WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Романтика');

//         INSERT INTO Genres (Name)
//         SELECT N'Пригоди'
//         WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Пригоди');

//         -- ФІЛЬМ
//         INSERT INTO Movies
//         (Title, Description, Duration, Releasedate, Rating, Posterurl, Trailerurl, Agerating, Languagecode, Countrycode, Isactive)
//         SELECT
//             N'Служниця',
//             N'Молода дівчина влаштовується працювати в маєток, але швидко розуміє, що за розкішшю ховаються темні таємниці.',
//             131,
//             '2025-12-19',
//             7.0,
//             'https://cdn.planetakino.ua/22583_the-housemaid_2025/Media/Posters/vertical/opt_856c0245-6081-46c9-a45c-198054195c4e.webp',
//             'https://www.youtube.com/watch?v=fDD-tAf88YM',
//             16,
//             'UA',
//             'US',
//             1
//         WHERE NOT EXISTS (
//             SELECT 1 FROM Movies WHERE Title = N'Служниця'
//         );

//         INSERT INTO Movies
//         (Title, Description, Duration, Releasedate, Rating, Posterurl, Trailerurl, Agerating, Languagecode, Countrycode, Isactive)
//         SELECT
//             N'Мавка. Лісова пісня',
//             N'Фентезійна історія кохання Мавки — лісової душі — та музиканта Лукаша. Давній конфлікт між людьми й природою постає перед вибором серця.',
//             99,
//             '2023-03-02',
//             7.5,
//             'https://upload.wikimedia.org/wikipedia/commons/e/e1/POSTER_MAVKA._THE_FOREST_SONG.png',
//             'https://www.youtube.com/watch?v=izQA8C3emt4',
//             6,
//             'UA',
//             'UA',
//             1
//         WHERE NOT EXISTS (
//             SELECT 1 FROM Movies WHERE Title = N'Мавка. Лісова пісня'
//         );



//         -- ЗВʼЯЗКИ
//         INSERT INTO Moviegenres (Movieid, Genreid)
//         SELECT m.Id, g.Id
//         FROM Movies m
//         JOIN Genres g ON g.Name = N'Трилер'
//         WHERE m.Title = N'Служниця'
//         AND NOT EXISTS (
//             SELECT 1 FROM Moviegenres
//             WHERE Movieid = m.Id AND Genreid = g.Id
//         );

//         INSERT INTO Moviegenres (Movieid, Genreid)
//         SELECT m.Id, g.Id
//         FROM Movies m
//         JOIN Genres g ON g.Name = N'Фантастика'
//         WHERE m.Title = N'Мавка. Лісова пісня'
//         AND NOT EXISTS (
//             SELECT 1 FROM Moviegenres
//             WHERE Movieid = m.Id AND Genreid = g.Id
//         );

//         INSERT INTO Moviegenres (Movieid, Genreid)
//         SELECT m.Id, g.Id
//         FROM Movies m
//         JOIN Genres g ON g.Name = N'Романтика'
//         WHERE m.Title = N'Мавка. Лісова пісня'
//         AND NOT EXISTS (
//             SELECT 1 FROM Moviegenres
//             WHERE Movieid = m.Id AND Genreid = g.Id
//         );

//         INSERT INTO Moviegenres (Movieid, Genreid)
//         SELECT m.Id, g.Id
//         FROM Movies m
//         JOIN Genres g ON g.Name = N'Пригоди'
//         WHERE m.Title = N'Мавка. Лісова пісня'
//         AND NOT EXISTS (
//             SELECT 1 FROM Moviegenres
//             WHERE Movieid = m.Id AND Genreid = g.Id
//         );

//     """);
// }

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
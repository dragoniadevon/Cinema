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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.ExecuteSqlRaw("""
        -- ЖАНРИ
        INSERT INTO Genres (Name)
        SELECT N'Драма'
        WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Драма');

        INSERT INTO Genres (Name)
        SELECT N'Комедія'
        WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Комедія');
        INSERT INTO Genres (Name)
        SELECT N'Фантастика'
        WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Фантастика');

        INSERT INTO Genres (Name)
        SELECT N'Трилер'
        WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Трилер');
        INSERT INTO Genres (Name)
        SELECT N'Романтика'
        WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Романтика');

        INSERT INTO Genres (Name)
        SELECT N'Пригоди'
        WHERE NOT EXISTS (SELECT 1 FROM Genres WHERE Name = N'Пригоди');

        -- ФІЛЬМ
        INSERT INTO Movies
        (Title, Description, Duration, Releasedate, Rating, Posterurl, Trailerurl, Agerating, Languagecode, Countrycode, Isactive)
        SELECT
            N'Служниця',
            N'Молода дівчина влаштовується працювати в маєток, але швидко розуміє, що за розкішшю ховаються темні таємниці.',
            131,
            '2025-12-19',
            7.0,
            'https://cdn.planetakino.ua/22583_the-housemaid_2025/Media/Posters/vertical/opt_856c0245-6081-46c9-a45c-198054195c4e.webp',
            'https://www.youtube.com/watch?v=fDD-tAf88YM',
            16,
            'UA',
            'US',
            1
        WHERE NOT EXISTS (
            SELECT 1 FROM Movies WHERE Title = N'Служниця'
        );

        INSERT INTO Movies
        (Title, Description, Duration, Releasedate, Rating, Posterurl, Trailerurl, Agerating, Languagecode, Countrycode, Isactive)
        SELECT
            N'Мавка. Лісова пісня',
            N'Фентезійна історія кохання Мавки — лісової душі — та музиканта Лукаша. Давній конфлікт між людьми й природою постає перед вибором серця.',
            99,
            '2023-03-02',
            7.5,
            'https://upload.wikimedia.org/wikipedia/commons/e/e1/POSTER_MAVKA._THE_FOREST_SONG.png',
            'https://www.youtube.com/watch?v=izQA8C3emt4',
            6,
            'UA',
            'UA',
            1
        WHERE NOT EXISTS (
            SELECT 1 FROM Movies WHERE Title = N'Мавка. Лісова пісня'
        );



        -- ЗВʼЯЗКИ
        INSERT INTO Moviegenres (Movieid, Genreid)
        SELECT m.Id, g.Id
        FROM Movies m
        JOIN Genres g ON g.Name = N'Трилер'
        WHERE m.Title = N'Служниця'
        AND NOT EXISTS (
            SELECT 1 FROM Moviegenres
            WHERE Movieid = m.Id AND Genreid = g.Id
        );

        INSERT INTO Moviegenres (Movieid, Genreid)
        SELECT m.Id, g.Id
        FROM Movies m
        JOIN Genres g ON g.Name = N'Фантастика'
        WHERE m.Title = N'Мавка. Лісова пісня'
        AND NOT EXISTS (
            SELECT 1 FROM Moviegenres
            WHERE Movieid = m.Id AND Genreid = g.Id
        );

        INSERT INTO Moviegenres (Movieid, Genreid)
        SELECT m.Id, g.Id
        FROM Movies m
        JOIN Genres g ON g.Name = N'Романтика'
        WHERE m.Title = N'Мавка. Лісова пісня'
        AND NOT EXISTS (
            SELECT 1 FROM Moviegenres
            WHERE Movieid = m.Id AND Genreid = g.Id
        );

        INSERT INTO Moviegenres (Movieid, Genreid)
        SELECT m.Id, g.Id
        FROM Movies m
        JOIN Genres g ON g.Name = N'Пригоди'
        WHERE m.Title = N'Мавка. Лісова пісня'
        AND NOT EXISTS (
            SELECT 1 FROM Moviegenres
            WHERE Movieid = m.Id AND Genreid = g.Id
        );

    """);
}


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
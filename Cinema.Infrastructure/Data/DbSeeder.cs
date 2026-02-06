using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Cinema.Infrastructure.Entities;
using Cinema.Infrastructure.Entities.Enums;


public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        //await SeedPriceCategoriesAsync(context);
        //await SeedRolesAsync(roleManager);
        //await SeedAdminAsync(userManager, roleManager);
        //await SeedCinemasAsync(context);
        await SeedGenresAsync(context);
        await SeedActorsAsync(context);
        await SeedMoviesAsync(context);

        await SeedMovieGenresAsync(context);
        await SeedMovieActorsAsync(context);
    }

    private static async Task SeedPriceCategoriesAsync(AppDbContext context)
    {
        if (await context.Pricecategories.AnyAsync())
            return;

        context.Pricecategories.AddRange(
            new Pricecategory { Name = "Standard" },
            new Pricecategory { Name = "VIP" }
        );

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        string[] roles = { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }

    private static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        string email = "admin@cinema.local";
        string password = "Admin123!";

        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Admin",
                LastName = "Cinema",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }

    private static async Task SeedCinemasAsync(AppDbContext context)
    {
        if (await context.Cinemas.AnyAsync(c =>
        c.Name == "Планета Кіно" && c.City == "Луцьк"))
        {
            return;
        }

        var cinema = new Cinema.Infrastructure.Entities.Cinema
        {
            Name = "Планета Кіно",
            City = "Луцьк",
            Address = "вул. Соборності, 11",
            Isactive = true
        };

        context.Cinemas.Add(cinema);
        await context.SaveChangesAsync();

        var halls = new[]
        {
        new { Name = "Зал №1 (Основний)", Type = (short)1, Rows = (short)10, Seats = (short)15 },
        new { Name = "Зал №2 (Стандарт)", Type = (short)2, Rows = (short)8, Seats = (short)10 },
        new { Name = "Зал №3 (VIP)", Type = (short)3, Rows = (short)4, Seats = (short)6 }
    };

        foreach (var h in halls)
        {
            var hall = new Hall
            {
                Name = h.Name,
                Cinemaid = cinema.Id,
                Halltype = h.Type,
                Rows = h.Rows,
                Seatsperrow = h.Seats,
                Isactive = true
            };

            context.Halls.Add(hall);
            await context.SaveChangesAsync();

            var seats = new List<Seat>();

            for (short r = 1; r <= hall.Rows; r++)
            {
                for (short s = 1; s <= hall.Seatsperrow; s++)
                {
                    seats.Add(new Seat
                    {
                        Hallid = hall.Id,
                        Rownumber = r,
                        Seatnumber = s
                    });
                }
            }

            context.Seats.AddRange(seats);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedGenresAsync(AppDbContext context)
    {
        var genresToSeed = new[]
        {
        "Біографічний",
        "Документальний",
        "Драма",
        "Екшн",
        "Жахи",
        "Комедія",
        "Концерт",
        "Пригоди",
        "Романтика",
        "Сімейний",
        "Спорт",
        "Трилер",
        "Фантастика",
        "Фентезі"
    };

        foreach (var name in genresToSeed)
        {
            var normalized = name.Trim();

            bool exists = await context.Genres
                .AnyAsync(g => g.Name.ToLower() == normalized.ToLower());

            if (!exists)
            {
                context.Genres.Add(new Genre
                {
                    Name = normalized
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedActorsAsync(AppDbContext context)
    {
        var actorsToSeed = new[]
        {
        "I.N",
        "Аманда Сейфрід",
        "Бан Чан",
        "Білл Найї",
        "Гвінет Пелтроу",
        "Денніс Хейсберт",
        "Джейкоб Елорді",
        "Джейсон Стейтем",
        "Ділан О'Брайен",
        "Елісон Олівер",
        "Елоді Фонтан",
        "Жамель Деббуз",
        "Жан Рено",
        "Кіану Рівз",
        "Кім Син Мін",
        "Лі Ноу",
        "Марґо Роббі",
        "Мартін Клунз",
        "Наомі Акі",
        "Рейчел МакАдамс",
        "Сідні Свіні",
        "Тарек Будалі",
        "Тімоті Шаламе",
        "Фелікс",
        "Філіпп Лашо",
        "Хан",
        "Хонґ Чау",
        "Хьонджін",
        "Чанбін",
        "Шазад Латіф",
        "Юен Мітчелта",
        "Деніел Редкліфф",
        "Емма Вотсон",
        "Руперт Грінт"
    };

        foreach (var fullName in actorsToSeed)
        {
            var normalized = fullName.Trim();

            bool exists = await context.Actors
                .AnyAsync(a => a.Fullname.ToLower() == normalized.ToLower());

            if (!exists)
            {
                context.Actors.Add(new Actor
                {
                    Fullname = normalized
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedMoviesAsync(AppDbContext context)
    {
        var moviesToSeed = new[]
        {
        new
        {
            Title = "Служниця",
            Description = "Міллі (Сідні Свінні) - молода дівчина з похмурим минулим майже втратила надію знайти роботу, аж допоки не отримує місце служниці з проживанням у розкішному маєтку родини Вінчестерів...",
            Duration = (short)131,
            Releasedate = new DateOnly(2025, 12, 19),
            Rating = (decimal?)7.0m,
            Posterurl = "https://cdn.planetakino.ua/22583_the-housemaid_2025/Media/Posters/vertical/opt_856c0245-6081-46c9-a45c-198054195c4e.webp",
            Trailerurl = "https://www.youtube.com/watch?v=fDD-tAf88YM",
            Agerating = AgeRating.R,            // 16+
            Languagecode = LanguageCode.UA,
            Countrycode = CountryCode.US,
            Isactive = true
        },
        new
        {
            Title = "Марті Супрім. Геній комбінацій",
            Description = "Марті — молодий та амбітний мрійник-серцеїд...",
            Duration = (short)150,
            Releasedate = new DateOnly(2026, 1, 15),
            Rating = (decimal?)8.2m,
            Posterurl = "https://multiplex.ua/images/b6/f2/b6f271d4c1771d1baf4db499eb1f521b.jpeg",
            Trailerurl = "https://www.youtube.com/watch?v=mOuoYpPCdkI",
            Agerating = AgeRating.R,            // 16+
            Languagecode = LanguageCode.UA,
            Countrycode = CountryCode.US,
            Isactive = false
        },
        new
        {
            Title = "Stray Kids: The dominATE Experience",
            Description = "Епічний фільм-концерт з ексклюзивними закулісними кадрами...",
            Duration = (short)156,
            Releasedate = new DateOnly(2026, 2, 5),
            Rating = (decimal?)null,
            Posterurl = "https://multiplex.ua/images/6b/a0/6ba05024b10b9df1dcbde0c5a4e7396c.jpeg",
            Trailerurl = "https://www.youtube.com/watch?v=EaJBlWwNQvI",
            Agerating = AgeRating.PG13,         // 12+
            Languagecode = LanguageCode.EN,
            Countrycode = CountryCode.US,
            Isactive = false
        },
        new
        {
            Title = "Буремний перевал",
            Description = "Сміливе й сучасне переосмислення класичної історії кохання...",
            Duration = (short)130,
            Releasedate = new DateOnly(2026, 2, 12),
            Rating = (decimal?)null,
            Posterurl = "https://cdn.planetakino.ua/18047_wuthering-heights_2025/Media/Posters/vertical/opt_04253747-8a6f-473c-8a6a-d597a1eb7c6f.webp",
            Trailerurl = "https://www.youtube.com/watch?v=yiG_Joj0134",
            Agerating = AgeRating.R,            // 16+
            Languagecode = LanguageCode.UA,
            Countrycode = CountryCode.US,
            Isactive = false
        },
        new
        {
            Title = "Марсупіламі. Хвостата халепа",
            Description = "Давид, щоб врятувати роботу, погоджується на авантюру: перевезти таємничий пакунок із Південної Америки...",
            Duration = (short)100,
            Releasedate = new DateOnly(2026, 1, 4),
            Rating = (decimal?)7.1m,
            Posterurl = "https://premierakino.com.ua/wp-content/uploads/2026/01/LE-MARSUPILAMI_250x336_Poster_UA-2-400x650.jpg",
            Trailerurl = "https://www.youtube.com/watch?v=WOhIm1W-UV4",
            Agerating = AgeRating.PG13,
            Languagecode = LanguageCode.UA,
            Countrycode = CountryCode.FR,
            Isactive = false
        },
        new
        {
            Title = "Гаррі Поттер і Таємна кімната",
            Description = "Гаррі Поттер повертається до школи чарівництва Гоґвортс на другий рік навчання, де його чекають нові пригоди та небезпеки...",
            Duration = (short)161,
            Releasedate = new DateOnly(2002, 11, 15),
            Rating = (decimal?)7.4m,
            Posterurl = "https://upload.wikimedia.org/wikipedia/uk/5/5f/%D0%93%D0%9F%D0%A4%D0%9A%D0%9F%D0%BE%D1%81%D1%82%D0%B5%D1%802.jpg",
            Trailerurl = "https://www.youtube.com/watch?v=KnJeMXtli3Q",
            Agerating = AgeRating.PG,
            Languagecode = LanguageCode.UA,
            Countrycode = CountryCode.GB,
            Isactive = true
        },
        new
        {
            Title = "Марті Супрім. Геній комбінацій",
            Description = "Марті — молодий та амбітний мрійник-серцеїд...",
            Duration = (short)150,
            Releasedate = new DateOnly(2026, 1, 15),
            Rating = (decimal?)8.2m,
            Posterurl = "https://multiplex.ua/images/b6/f2/b6f271d4c1771d1baf4db499eb1f521b.jpeg",
            Trailerurl = "https://www.youtube.com/watch?v=mOuoYpPCdkI",
            Agerating = AgeRating.R,            // 16+
            Languagecode = LanguageCode.UA,
            Countrycode = CountryCode.US,
            Isactive = false
        },
        new
        {
            Title = "Stray Kids: The dominATE Experience",
            Description = "Епічний фільм-концерт з ексклюзивними закулісними кадрами...",
            Duration = (short)156,
            Releasedate = new DateOnly(2026, 2, 5),
            Rating = (decimal?)null,
            Posterurl = "https://multiplex.ua/images/6b/a0/6ba05024b10b9df1dcbde0c5a4e7396c.jpeg",
            Trailerurl = "https://www.youtube.com/watch?v=EaJBlWwNQvI",
            Agerating = AgeRating.PG13,         // 12+
            Languagecode = LanguageCode.EN,
            Countrycode = CountryCode.US,
            Isactive = false
        },
        new
        {
            Title = "Самотник",
            Description = "Після смерті матері молодий чоловік повертається до рідного міста, щоб розібратися з минулим та знайти своє місце в світі...",
            Duration = (short)120,
            Releasedate = new DateOnly(2026, 1, 29),
            Rating = (decimal?)6.5m,
            Posterurl = "https://multiplex.ua/images/e5/ea/e5ea41badfc327b8a3e99fe2d0c11ee3.jpeg",
            Trailerurl = "https://www.youtube.com/watch?v=Ei72RQ1z_YI&t=1s",
            Agerating = AgeRating.R,            // 16+
            Languagecode = LanguageCode.UA,
            Countrycode = CountryCode.US,
            Isactive = false
        },
        new
        {
            Title = "Допоможіть",
            Description = "Двоє колег стають єдиними, хто вижив після авіакатастрофи. Опинившись на безлюдному острові, вони мусять подолати давні образи та навчитися діяти разом, щоб вижити...",
            Duration = (short)120,
            Releasedate = new DateOnly(2026, 1, 29),
            Rating = (decimal?)7.4m,
            Posterurl = "https://multiplex.ua/images/88/ef/88ef9d8d9563cebd623b09b192b55f3d.jpeg",
            Trailerurl = "https://www.youtube.com/watch?v=Oyx8rJet8cQ",
            Agerating = AgeRating.R,            // 16+
            Languagecode = LanguageCode.UA,
            Countrycode = CountryCode.US,
            Isactive = true
        }
    };

        foreach (var m in moviesToSeed)
        {
            bool exists = await context.Movies.AnyAsync(movie =>
                movie.Title == m.Title &&
                movie.Releasedate == m.Releasedate);

            if (!exists)
            {
                context.Movies.Add(new Movie
                {
                    Title = m.Title,
                    Description = m.Description,
                    Duration = m.Duration,
                    Releasedate = m.Releasedate,
                    Rating = m.Rating,
                    Posterurl = m.Posterurl,
                    Trailerurl = m.Trailerurl,
                    Agerating = m.Agerating,
                    Languagecode = m.Languagecode,
                    Countrycode = m.Countrycode,
                    Isactive = m.Isactive
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedMovieGenresAsync(AppDbContext context)
    {
        await AddGenresToMovie(
            context,
            title: "Служниця",
            releaseDate: new DateOnly(2025, 12, 19),
            genreNames: new[] { "Драма", "Трилер", "Романтика" }
        );

        await AddGenresToMovie(
            context,
            title: "Буремний перевал",
            releaseDate: new DateOnly(2026, 2, 12),
            genreNames: new[] { "Драма", "Романтика" }
        );

        await AddGenresToMovie(
            context,
            title: "Марсупіламі. Хвостата халепа",
            releaseDate: new DateOnly(2026, 1, 4),
            genreNames: new[] { "Комедія", "Пригоди", "Сімейний" }
        );

        await AddGenresToMovie(
            context,
            title: "Гаррі Поттер і Таємна кімната",
            releaseDate: new DateOnly(2002, 11, 15),
            genreNames: new[] { "Пригоди", "Фентезі", "Сімейний" }
        );

        await AddGenresToMovie(
            context,
            title: "Марті Супрім. Геній комбінацій",
            releaseDate: new DateOnly(2026, 1, 15),
            genreNames: new[] { "Біографічний", "Драма" }
        );
        await AddGenresToMovie(
            context,
            title: "Stray Kids: The dominATE Experience",
            releaseDate: new DateOnly(2026, 2, 5),
            genreNames: new[] { "Концерт", "Документальний" }
        );
        await AddGenresToMovie(
            context,
            title: "Самотник",
            releaseDate: new DateOnly(2026, 1, 29),
            genreNames: new[] { "Екшн", "Трилер" }
        );
        await AddGenresToMovie(
            context,
            title: "Допоможіть",
            releaseDate: new DateOnly(2026, 1, 29),
            genreNames: new[] { "Жахи", "Трилер" }
        );
    }

    private static async Task AddGenresToMovie(
        AppDbContext context,
        string title,
        DateOnly releaseDate,
        string[] genreNames)
    {
        var movie = await context.Movies.FirstOrDefaultAsync(m =>
            m.Title == title && m.Releasedate == releaseDate);

        if (movie == null)
            return;

        foreach (var genreName in genreNames)
        {
            var genre = await context.Genres
                .FirstOrDefaultAsync(g => g.Name == genreName);

            if (genre == null)
                continue;

            bool exists = await context.Moviegenres.AnyAsync(mg =>
                mg.Movieid == movie.Id &&
                mg.Genreid == genre.Id);

            if (!exists)
            {
                context.Moviegenres.Add(new Moviegenre
                {
                    Movieid = movie.Id,
                    Genreid = genre.Id
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedMovieActorsAsync(AppDbContext context)
    {
        await AddActorsToMovie(
            context,
            title: "Служниця",
            releaseDate: new DateOnly(2025, 12, 19),
            actorNames: new[]
            {
            "Сідні Свіні",
            "Аманда Сейфрід"
            }
        );

        await AddActorsToMovie(
            context,
            title: "Буремний перевал",
            releaseDate: new DateOnly(2026, 2, 12),
            actorNames: new[]
            {
            "Марґо Роббі",
            "Джейкоб Елорді"
            }
        );
        await AddActorsToMovie(
            context,
            title: "Марсупіламі. Хвостата халепа",
            releaseDate: new DateOnly(2026, 1, 4),
            actorNames: new[]
            {
            "Жан Рено",
            "Жамель Деббуз",
            "Елоді Фонтан",
            "Тарек Будалі",
            "Філіпп Лашо"
            }
        );
        await AddActorsToMovie(
            context,
            title: "Гаррі Поттер і Таємна кімната",
            releaseDate: new DateOnly(2002, 11, 15),
            actorNames: new[]
            {
            "Деніел Редкліфф",
            "Емма Вотсон",
            "Руперт Грінт"
            }
        );
        await AddActorsToMovie(
            context,
            title: "Марті Супрім. Геній комбінацій",
            releaseDate: new DateOnly(2026, 1, 15),
            actorNames: new[]
            {
            "Тімоті Шаламе",
            "Гвінет Пелтроу"
            }
        );
        await AddActorsToMovie(
            context,
            title: "Stray Kids: The dominATE Experience",
            releaseDate: new DateOnly(2026, 2, 5),
            actorNames: new[]
            {
            "I.N",
            "Бан Чан",
            "Кім Син Мін",
            "Лі Ноу",
            "Фелікс",
            "Хан",
            "Хьонджін",
            "Чанбін"
            }
        );
        await AddActorsToMovie(
            context,
            title: "Самотник",
            releaseDate: new DateOnly(2026, 1, 29),
            actorNames: new[]
            {
            "Джейсон Стейтем",
            "Білл Найї",
            "Наомі Акі"
            }
        );
        await AddActorsToMovie(
            context,
            title: "Допоможіть",
            releaseDate: new DateOnly(2026, 1, 29),
            actorNames: new[]
            {
            "Рейчел МакАдамс",
            "Ділан О'Брайен",
            "Денніс Хейсберт"
            }
        );
    }

    private static async Task AddActorsToMovie(
        AppDbContext context,
        string title,
        DateOnly releaseDate,
        string[] actorNames)
    {
        var movie = await context.Movies.FirstOrDefaultAsync(m =>
            m.Title == title && m.Releasedate == releaseDate);

        if (movie == null)
            return;

        foreach (var actorName in actorNames)
        {
            var actor = await context.Actors
                .FirstOrDefaultAsync(a => a.Fullname == actorName);

            if (actor == null)
                continue;

            bool exists = await context.Movieactors.AnyAsync(ma =>
                ma.Movieid == movie.Id &&
                ma.Actorid == actor.Id);

            if (!exists)
            {
                context.Movieactors.Add(new Movieactor
                {
                    Movieid = movie.Id,
                    Actorid = actor.Id
                });
            }
        }

        await context.SaveChangesAsync();
    }

}



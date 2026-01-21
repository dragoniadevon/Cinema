using Cinema.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Налаштування сервісів
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();

// Підключення до PostgreSQL (Supabase)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(30);
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null
            );
        }));


var app = builder.Build();

// =======================
// SEED TEST DATA (Movies)
// =======================
// using (var scope = app.Services.CreateScope())
// {
//     var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//     // Wake up Supabase connection
//     context.Database.OpenConnection();
//     context.Database.CloseConnection();

//     if (!context.Movies.Any())
//     {
//         context.Movies.AddRange(
//             new Movie
//             {
//                 Title = "Interstellar",
//                 Description = "Science fiction film about space and time.",
//                 Duration = 169,
//                 Rating = 8.6m,
//                 Releasedate = new DateOnly(2014, 11, 7),
//                 Languagecode = "EN",
//                 Countrycode = "US",
//                 Isactive = true
//             },
//             new Movie
//             {
//                 Title = "Inception",
//                 Description = "A mind-bending thriller about dreams within dreams.",
//                 Duration = 148,
//                 Rating = 8.8m,
//                 Releasedate = new DateOnly(2010, 7, 16),
//                 Languagecode = "EN",
//                 Countrycode = "US",
//                 Isactive = true
//             }
//         );

//         context.SaveChanges();
//     }
// }
// =======================
// END SEED
// =======================

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
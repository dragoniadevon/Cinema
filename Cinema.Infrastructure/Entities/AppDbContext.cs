using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Entities;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Movieactor> Movieactors => Set<Movieactor>();
    public DbSet<Pricecategory> Pricecategories => Set<Pricecategory>();
    public DbSet<Sessionprice> Sessionprices => Set<Sessionprice>();
    public DbSet<Usergenrepreference> Usergenrepreferences => Set<Usergenrepreference>();

    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<Moviegenre> Moviegenres => Set<Moviegenre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Movie
        modelBuilder.Entity<Movie>()
            .Property(m => m.Rating)
            .HasPrecision(3, 1);

        modelBuilder.Entity<Movie>()
        .Property(m => m.Agerating)
        .HasConversion<short>();

        modelBuilder.Entity<Movie>()
            .Property(m => m.Languagecode)
            .HasConversion<string>();

        modelBuilder.Entity<Movie>()
            .Property(m => m.Countrycode)
            .HasConversion<string>();


        // Payment
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(10, 2);

        // SessionPrice
        modelBuilder.Entity<Sessionprice>()
            .Property(sp => sp.Price)
            .HasPrecision(10, 2);

        // Ticket
        modelBuilder.Entity<Ticket>()
            .Property(t => t.Price)
            .HasPrecision(10, 2);

        // Movie ↔ Genre (many-to-many)
        modelBuilder.Entity<Moviegenre>()
            .HasKey(mg => new { mg.Movieid, mg.Genreid });

        modelBuilder.Entity<Moviegenre>()
            .HasOne(mg => mg.Movie)
            .WithMany(m => m.MovieGenres)
            .HasForeignKey(mg => mg.Movieid);

        modelBuilder.Entity<Moviegenre>()
            .HasOne(mg => mg.Genre)
            .WithMany(g => g.MovieGenres)
            .HasForeignKey(mg => mg.Genreid);

        // Унікальні місця в залі
        modelBuilder.Entity<Seat>()
            .HasIndex(s => new { s.Hallid, s.Rownumber, s.Seatnumber })
            .IsUnique();

        // Заборона подвійного продажу місця
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.Sessionid, t.Seatid })
            .IsUnique();

        // User ↔ Genre preferences
        modelBuilder.Entity<Usergenrepreference>()
            .HasKey(x => new { x.Userid, x.Genreid });

        modelBuilder.Entity<Movieactor>()
            .HasKey(ma => new { ma.Movieid, ma.Actorid });

        modelBuilder.Entity<Movieactor>()
            .HasOne(ma => ma.Movie)
            .WithMany(m => m.MovieActors)
            .HasForeignKey(ma => ma.Movieid);

        modelBuilder.Entity<Movieactor>()
            .HasOne(ma => ma.Actor)
            .WithMany(a => a.Movieactors)
            .HasForeignKey(ma => ma.Actorid);

    }
}

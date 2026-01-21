using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Entities;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cinema> Cinemas { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Hall> Halls { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<Movieperson> Moviepersons { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Person> Persons { get; set; }

    public virtual DbSet<Personrole> Personroles { get; set; }

    public virtual DbSet<Pricecategory> Pricecategories { get; set; }

    public virtual DbSet<Recommendation> Recommendations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Sessionprice> Sessionprices { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Usergenrepreference> Usergenrepreferences { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=aws-1-eu-central-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.aqfvdoidwibocqlzxrjr;Password=y537%ps5Uh%b5@_;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("graphql", "pg_graphql")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cinemas_pkey");

            entity.ToTable("cinemas");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasColumnName("address");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("genres_pkey");

            entity.ToTable("genres");

            entity.HasIndex(e => e.Name, "genres_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Hall>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("halls_pkey");

            entity.ToTable("halls");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Cinemaid).HasColumnName("cinemaid");
            entity.Property(e => e.Halltype).HasColumnName("halltype");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Rows).HasColumnName("rows");
            entity.Property(e => e.Seatsperrow).HasColumnName("seatsperrow");

            entity.HasOne(d => d.Cinema).WithMany(p => p.Halls)
                .HasForeignKey(d => d.Cinemaid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("halls_cinemaid_fkey");
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("movies_pkey");

            entity.ToTable("movies");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Agerating).HasColumnName("agerating");
            entity.Property(e => e.Countrycode)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("countrycode");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.Isactive)
                .HasDefaultValue(true)
                .HasColumnName("isactive");
            entity.Property(e => e.Languagecode)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("languagecode");
            entity.Property(e => e.Posterurl)
                .HasMaxLength(500)
                .HasColumnName("posterurl");
            entity.Property(e => e.Rating)
                .HasPrecision(3, 1)
                .HasColumnName("rating");
            entity.Property(e => e.Releasedate).HasColumnName("releasedate");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.Trailerurl)
                .HasMaxLength(500)
                .HasColumnName("trailerurl");

            entity.HasMany(d => d.Genres).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "Moviegenre",
                    r => r.HasOne<Genre>().WithMany()
                        .HasForeignKey("Genreid")
                        .HasConstraintName("moviegenres_genreid_fkey"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("Movieid")
                        .HasConstraintName("moviegenres_movieid_fkey"),
                    j =>
                    {
                        j.HasKey("Movieid", "Genreid").HasName("moviegenres_pkey");
                        j.ToTable("moviegenres");
                        j.IndexerProperty<int>("Movieid").HasColumnName("movieid");
                        j.IndexerProperty<int>("Genreid").HasColumnName("genreid");
                    });
        });

        modelBuilder.Entity<Movieperson>(entity =>
        {
            entity.HasKey(e => new { e.Movieid, e.Personid, e.Roleid }).HasName("moviepersons_pkey");

            entity.ToTable("moviepersons");

            entity.Property(e => e.Movieid).HasColumnName("movieid");
            entity.Property(e => e.Personid).HasColumnName("personid");
            entity.Property(e => e.Roleid).HasColumnName("roleid");

            entity.HasOne(d => d.Movie).WithMany(p => p.Moviepeople)
                .HasForeignKey(d => d.Movieid)
                .HasConstraintName("moviepersons_movieid_fkey");

            entity.HasOne(d => d.Person).WithMany(p => p.Moviepeople)
                .HasForeignKey(d => d.Personid)
                .HasConstraintName("moviepersons_personid_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Moviepeople)
                .HasForeignKey(d => d.Roleid)
                .HasConstraintName("moviepersons_roleid_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.HasIndex(e => e.Ticketid, "payments_ticketid_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Paymentdate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("paymentdate");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Ticketid).HasColumnName("ticketid");

            entity.HasOne(d => d.Ticket).WithOne(p => p.Payment)
                .HasForeignKey<Payment>(d => d.Ticketid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("payments_ticketid_fkey");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("persons_pkey");

            entity.ToTable("persons");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Fullname)
                .HasMaxLength(150)
                .HasColumnName("fullname");
        });

        modelBuilder.Entity<Personrole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("personroles_pkey");

            entity.ToTable("personroles");

            entity.HasIndex(e => e.Name, "personroles_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Pricecategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pricecategories_pkey");

            entity.ToTable("pricecategories");

            entity.HasIndex(e => e.Name, "pricecategories_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recommendations_pkey");

            entity.ToTable("recommendations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("createdat");
            entity.Property(e => e.Movieid).HasColumnName("movieid");
            entity.Property(e => e.Reason)
                .HasMaxLength(200)
                .HasColumnName("reason");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Movie).WithMany(p => p.Recommendations)
                .HasForeignKey(d => d.Movieid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("recommendations_movieid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Recommendations)
                .HasForeignKey(d => d.Userid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("recommendations_userid_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("seats_pkey");

            entity.ToTable("seats");

            entity.HasIndex(e => new { e.Hallid, e.Rownumber, e.Seatnumber }, "seats_hallid_rownumber_seatnumber_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Hallid).HasColumnName("hallid");
            entity.Property(e => e.Rownumber).HasColumnName("rownumber");
            entity.Property(e => e.Seatnumber).HasColumnName("seatnumber");

            entity.HasOne(d => d.Hall).WithMany(p => p.Seats)
                .HasForeignKey(d => d.Hallid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("seats_hallid_fkey");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sessions_pkey");

            entity.ToTable("sessions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Endtime).HasColumnName("endtime");
            entity.Property(e => e.Format).HasColumnName("format");
            entity.Property(e => e.Hallid).HasColumnName("hallid");
            entity.Property(e => e.Isactive)
                .HasDefaultValue(true)
                .HasColumnName("isactive");
            entity.Property(e => e.Language)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("language");
            entity.Property(e => e.Movieid).HasColumnName("movieid");
            entity.Property(e => e.Starttime).HasColumnName("starttime");

            entity.HasOne(d => d.Hall).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.Hallid)
                .HasConstraintName("sessions_hallid_fkey");

            entity.HasOne(d => d.Movie).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.Movieid)
                .HasConstraintName("sessions_movieid_fkey");
        });

        modelBuilder.Entity<Sessionprice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sessionprices_pkey");

            entity.ToTable("sessionprices");

            entity.HasIndex(e => new { e.Sessionid, e.Categoryid }, "sessionprices_sessionid_categoryid_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Sessionid).HasColumnName("sessionid");

            entity.HasOne(d => d.Category).WithMany(p => p.Sessionprices)
                .HasForeignKey(d => d.Categoryid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sessionprices_categoryid_fkey");

            entity.HasOne(d => d.Session).WithMany(p => p.Sessionprices)
                .HasForeignKey(d => d.Sessionid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sessionprices_sessionid_fkey");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tickets_pkey");

            entity.ToTable("tickets");

            entity.HasIndex(e => new { e.Sessionid, e.Seatid }, "tickets_sessionid_seatid_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Bookingtime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("bookingtime");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Seatid).HasColumnName("seatid");
            entity.Property(e => e.Sessionid).HasColumnName("sessionid");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Seat).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.Seatid)
                .HasConstraintName("tickets_seatid_fkey");

            entity.HasOne(d => d.Session).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.Sessionid)
                .HasConstraintName("tickets_sessionid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("tickets_userid_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("createdat");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.Firstname)
                .HasMaxLength(50)
                .HasColumnName("firstname");
            entity.Property(e => e.Lastname)
                .HasMaxLength(50)
                .HasColumnName("lastname");
            entity.Property(e => e.Passwordhash)
                .HasMaxLength(256)
                .HasColumnName("passwordhash");
            entity.Property(e => e.Roleid).HasColumnName("roleid");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.Roleid)
                .HasConstraintName("users_roleid_fkey");
        });

        modelBuilder.Entity<Usergenrepreference>(entity =>
        {
            entity.HasKey(e => new { e.Userid, e.Genreid }).HasName("usergenrepreferences_pkey");

            entity.ToTable("usergenrepreferences");

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Genreid).HasColumnName("genreid");
            entity.Property(e => e.Score)
                .HasDefaultValue((short)0)
                .HasColumnName("score");

            entity.HasOne(d => d.Genre).WithMany(p => p.Usergenrepreferences)
                .HasForeignKey(d => d.Genreid)
                .HasConstraintName("usergenrepreferences_genreid_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Usergenrepreferences)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("usergenrepreferences_userid_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

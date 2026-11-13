using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace CinemaTicketBookingSystem.Entities;

public partial class CinemaManagementDbContext : DbContext
{
    public CinemaManagementDbContext()
    {
    }

    public CinemaManagementDbContext(DbContextOptions<CinemaManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingFood> BookingFoods { get; set; }

    public virtual DbSet<FoodAndDrink> FoodAndDrinks { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Showtime> Showtimes { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(
     DbContextOptionsBuilder optionsBuilder)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: true)
            .Build();

        string? strConn = config["ConnectionStrings:DefaultConnection"];

        optionsBuilder.UseSqlServer(strConn);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__73951AEDF6704071");

            entity.ToTable("Booking");

            entity.HasIndex(e => e.QrcodeString, "UQ__Booking__FAB83BC75594085A").IsUnique();

            entity.Property(e => e.BookingDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.QrcodeString)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("QRCodeString");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50).HasDefaultValue("VNPAY");

            entity.HasOne(d => d.Showtime).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__Showtim__3E52440B");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__UserId__3D5E1FD2");
        });

        modelBuilder.Entity<BookingFood>(entity =>
        {
            entity.HasKey(e => e.BookingFoodId).HasName("PK__BookingF__8553E778751A1FCD");

            entity.ToTable("BookingFood");

            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingFoods)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BookingFo__Booki__46E78A0C");

            entity.HasOne(d => d.Food).WithMany(p => p.BookingFoods)
                .HasForeignKey(d => d.FoodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BookingFo__FoodI__47DBAE45");
        });

        modelBuilder.Entity<FoodAndDrink>(entity =>
        {
            entity.HasKey(e => e.FoodId).HasName("PK__FoodAndD__856DB3EB723383FB");

            entity.ToTable("FoodAndDrink");

            entity.Property(e => e.FoodName).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.Status).HasDefaultValue(true);
            entity.Property(e => e.Stock).HasDefaultValue(100);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.GenreId).HasName("PK__Genre__0385057EE062CCF4");

            entity.ToTable("Genre");

            entity.Property(e => e.GenreName).HasMaxLength(50);
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.MovieId).HasName("PK__Movie__4BD2941A769637C1");

            entity.ToTable("Movie");

            entity.Property(e => e.AgeRating)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("P");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PosterPath).HasMaxLength(255);
            entity.Property(e => e.Status).HasDefaultValue(true);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.Genre).WithMany(p => p.Movies)
                .HasForeignKey(d => d.GenreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Movie__GenreId__2B3F6F97");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Room__32863939C3299509");

            entity.ToTable("Room");

            entity.Property(e => e.RoomName).HasMaxLength(50);
            entity.Property(e => e.Status).HasDefaultValue(true);
            entity.Property(e => e.TotalSeats).HasDefaultValue(40);
        });

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.HasKey(e => e.ShowtimeId).HasName("PK__Showtime__32D31F20CC779606");

            entity.ToTable("Showtime");

            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasDefaultValue(true);

            entity.HasOne(d => d.Movie).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Showtime__MovieI__33D4B598");

            entity.HasOne(d => d.Room).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Showtime__RoomId__34C8D9D1");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__Ticket__712CC607815F6B2B");

            entity.ToTable("Ticket");

            entity.HasIndex(e => new { e.ShowtimeId, e.SeatCode }, "UQ_Ticket_Showtime_Seat").IsUnique();

            entity.Property(e => e.SeatCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.SeatPrice).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Booking).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ticket__BookingI__4316F928");

            entity.HasOne(d => d.Showtime).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ticket__Showtime__440B1D61");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4C87B84F08");

            entity.ToTable("User");

            entity.HasIndex(e => e.Username, "UQ__User__536C85E4415CC20B").IsUnique();

            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Customer");
            entity.Property(e => e.Status).HasDefaultValue(true);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

using System;
using System.Collections.Generic;

namespace CinemaTicketBookingSystem.Entities;

public partial class Booking
{
    public int BookingId { get; set; }

    public int UserId { get; set; }

    public int ShowtimeId { get; set; }

    public DateTime BookingDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string QrcodeString { get; set; } = null!;

    public bool IsCheckedIn { get; set; }

    public virtual ICollection<BookingFood> BookingFoods { get; set; } = new List<BookingFood>();

    public virtual Showtime Showtime { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual User User { get; set; } = null!;
}

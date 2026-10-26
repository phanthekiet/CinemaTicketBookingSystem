using System;
using System.Collections.Generic;

namespace CinemaTicketBookingSystem.Entities;

public partial class Ticket
{
    public int TicketId { get; set; }

    public int BookingId { get; set; }

    public int ShowtimeId { get; set; }

    public string SeatCode { get; set; } = null!;

    public decimal SeatPrice { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Showtime Showtime { get; set; } = null!;
}

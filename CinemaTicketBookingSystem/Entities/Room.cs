using System;
using System.Collections.Generic;

namespace CinemaTicketBookingSystem.Entities;

public partial class Room
{
    public int RoomId { get; set; }

    public string RoomName { get; set; } = null!;

    public int TotalSeats { get; set; }

    public bool Status { get; set; }

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}

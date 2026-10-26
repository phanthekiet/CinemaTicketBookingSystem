using System;
using System.Collections.Generic;

namespace CinemaTicketBookingSystem.Entities;

public partial class Genre
{
    public int GenreId { get; set; }

    public string GenreName { get; set; } = null!;

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}

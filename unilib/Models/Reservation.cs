namespace unilib.Models;

// Status: see ReservationStatus.
public class Reservation
{
    public int Id { get; set; }
    public string BookId { get; set; } = null!;
    public int UserId { get; set; }
    public DateOnly ReservationDate { get; set; }
    public string Status { get; set; } = ReservationStatus.Pending;

    public Book Book { get; set; } = null!;
    public User User { get; set; } = null!;
}

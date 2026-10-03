namespace unilib.Models;

public class Favorite
{
    public int UserId { get; set; }
    public string BookId { get; set; } = null!;
    public DateOnly AddedAt { get; set; }

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}

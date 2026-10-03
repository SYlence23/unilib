namespace unilib.Models;

public class Library
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

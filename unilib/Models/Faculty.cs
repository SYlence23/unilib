namespace unilib.Models;

public class Faculty
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

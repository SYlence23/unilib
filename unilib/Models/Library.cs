namespace unilib.Models;

public class Library
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    // WGS 84 coordinates in degrees; either both are set or both are null.
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

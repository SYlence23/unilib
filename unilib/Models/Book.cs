namespace unilib.Models;

public class Book
{
    public string Isbn { get; set; } = null!;
    public int? FacultyId { get; set; }
    public int? LibraryId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = null!;
    public int? PublicationYear { get; set; }
    public int? Pages { get; set; }
    public string? Language { get; set; }
    public string? Description { get; set; }
    public int? Amount { get; set; }

    public Faculty? Faculty { get; set; }
    public Library? Library { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    public ICollection<UserBook> Loans { get; set; } = new List<UserBook>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}

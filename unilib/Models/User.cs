namespace unilib.Models;

public class User
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int? FacultyId { get; set; }
    public int? LibraryId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? StudentId { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Role Role { get; set; } = null!;
    public Faculty? Faculty { get; set; }
    public Library? Library { get; set; }
    public ICollection<Author> Authors { get; set; } = new List<Author>();
    public ICollection<UserBook> Loans { get; set; } = new List<UserBook>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}

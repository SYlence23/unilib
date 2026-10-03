namespace unilib.Models;

// A book loan. Status: see LoanStatus.
public class UserBook
{
    public int Id { get; set; }
    public string BookIsbn { get; set; } = null!;
    public int UserId { get; set; }
    public DateOnly BorrowDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public string Status { get; set; } = LoanStatus.Active;

    public Book Book { get; set; } = null!;
    public User User { get; set; } = null!;
}

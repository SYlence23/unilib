namespace unilib.Models;

public class BookAuthor
{
    public string BookId { get; set; } = null!;
    public int AuthorId { get; set; }

    public Book Book { get; set; } = null!;
    public Author Author { get; set; } = null!;
}

using unilib.Data;
using unilib.Models;
using unilib.Services;

namespace unilib.UI;

// The database has no shelf positions yet, so books are shelved by category:
// every category gets one shelf (A1..A6, then B1..B6) in each library.
public static class Shelving
{
    public static readonly string[] Codes = { "A1", "A2", "A3", "A4", "A5", "A6", "B1", "B2", "B3", "B4", "B5", "B6" };

    public static string ShelfFor(int categoryId, IReadOnlyList<Category> categories)
    {
        var index = categories.OrderBy(c => c.Id).ToList().FindIndex(c => c.Id == categoryId);
        return Codes[Math.Max(index, 0) % Codes.Length];
    }
}

// Everything the catalog, profile and map screens show, loaded in one go for the signed-in user.
public sealed class CatalogData
{
    private Dictionary<string, int> _onLoan = new();
    private HashSet<string> _saved = new();
    // Null uses the app database; tests pass their own.
    private Func<LibraryDbContext>? _createDb;

    public List<Book> Books { get; private set; } = new();
    public List<Category> Categories { get; private set; } = new();
    public List<Library> Libraries { get; private set; } = new();
    public List<Reservation> MyReservations { get; private set; } = new();
    public List<UserBook> MyActiveLoans { get; private set; } = new();

    public static async Task<CatalogData> LoadAsync(int userId, Func<LibraryDbContext>? createDb = null)
    {
        var books = new BookService(createDb).GetAllAsync();
        var categories = new CategoryService(createDb).GetAllAsync();
        var libraries = new LibraryService(createDb).GetAllAsync();
        var activeLoans = new LoanService(createDb).GetActiveAsync();
        var reservations = new ReservationService(createDb).GetByUserAsync(userId);
        var favorites = new FavoriteService(createDb).GetByUserAsync(userId);
        await Task.WhenAll(books, categories, libraries, activeLoans, reservations, favorites);

        return new CatalogData
        {
            _createDb = createDb,
            Books = books.Result,
            Categories = categories.Result,
            Libraries = libraries.Result,
            MyReservations = reservations.Result,
            MyActiveLoans = activeLoans.Result.Where(l => l.UserId == userId).ToList(),
            _onLoan = activeLoans.Result.GroupBy(l => l.BookIsbn).ToDictionary(g => g.Key, g => g.Count()),
            _saved = favorites.Result.Select(f => f.BookId).ToHashSet()
        };
    }

    // Copies not currently on loan.
    public int Available(Book book) => Math.Max((book.Amount ?? 0) - _onLoan.GetValueOrDefault(book.Isbn), 0);

    public string AvailabilityLabel(Book book)
    {
        var available = Available(book);
        return available > 0 ? $"{available} available" : "All copies on loan";
    }

    // The user already has a pending or approved reservation for this book.
    public bool IsHeld(string isbn) => MyReservations.Any(r => r.BookId == isbn
        && ReservationStatus.IsOpen(r.Status));

    public bool IsSaved(string isbn) => _saved.Contains(isbn);

    public void SetSaved(string isbn, bool saved)
    {
        if (saved) _saved.Add(isbn);
        else _saved.Remove(isbn);
    }

    // Pending and approved requests both count toward the limit, as in IsHeld.
    public int OpenReservations => MyReservations.Count(r => ReservationStatus.IsOpen(r.Status));

    // Creates a pending reservation and returns the message to show the user.
    public async Task<string> ReserveAsync(int userId, Book book)
    {
        if (IsHeld(book.Isbn))
            return "You have already reserved this book.";
        if (OpenReservations >= LibraryRules.MaxOpenReservations)
            return ReservationService.LimitReachedMessage;

        var reservation = await new ReservationService(_createDb).CreateAsync(userId, book.Isbn);
        reservation.Book = book;
        MyReservations.Insert(0, reservation);
        return $"Book reserved! {Ui.RequestId(reservation.Id)} is pending approval.";
    }

    // Adds the book to the user's saved books, or removes it. Returns whether it is saved now.
    public async Task<bool> ToggleSavedAsync(int userId, Book book)
    {
        var favorites = new FavoriteService(_createDb);
        var save = !IsSaved(book.Isbn);
        if (save)
            await favorites.AddAsync(userId, book.Isbn);
        else
            await favorites.RemoveAsync(userId, book.Isbn);
        SetSaved(book.Isbn, save);
        return save;
    }

    public string? Shelf(Book book) => book.LibraryId == null ? null : Shelving.ShelfFor(book.CategoryId, Categories);

    public string Location(Book book) => book.Library == null
        ? "Location not assigned"
        : $"{book.Library.Name} · Shelf {Shelf(book)}";

    public static string Authors(Book book)
    {
        var names = book.BookAuthors
            .Select(ba => $"{ba.Author.FirstName} {ba.Author.LastName}".Trim())
            .Where(n => n.Length > 0)
            .ToList();
        return names.Count > 0 ? string.Join(", ", names) : "Unknown author";
    }
}

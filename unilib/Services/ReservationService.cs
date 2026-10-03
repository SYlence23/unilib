using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

// Status flow: pending -> active (confirmed) -> completed; pending/active -> cancelled.
public class ReservationService : ServiceBase
{
    public ReservationService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<List<Reservation>> GetAllAsync()
    {
        using var db = CreateDb();
        return await WithDetails(db.Reservations.AsNoTracking())
            .OrderByDescending(r => r.ReservationDate)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        using var db = CreateDb();
        return await WithDetails(db.Reservations.AsNoTracking()).SingleOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Reservation>> GetByUserAsync(int userId)
    {
        using var db = CreateDb();
        return await WithDetails(db.Reservations.AsNoTracking())
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReservationDate)
            .ToListAsync();
    }

    // Requests waiting for a librarian.
    public async Task<List<Reservation>> GetPendingAsync()
    {
        using var db = CreateDb();
        return await WithDetails(db.Reservations.AsNoTracking())
            .Where(r => r.Status == ReservationStatus.Pending)
            .OrderBy(r => r.ReservationDate)
            .ToListAsync();
    }

    public async Task<Reservation> CreateAsync(int userId, string isbn)
    {
        using var db = CreateDb();
        if (!await db.Users.AnyAsync(u => u.Id == userId))
            throw NotFound<User>(userId);
        if (!await db.Books.AnyAsync(b => b.Isbn == isbn))
            throw NotFound<Book>(isbn);

        var reservation = new Reservation
        {
            UserId = userId,
            BookId = isbn,
            ReservationDate = DateOnly.FromDateTime(DateTime.Today),
            Status = ReservationStatus.Pending
        };

        db.Reservations.Add(reservation);
        await SaveAsync(db, nameof(Reservation));
        return reservation;
    }

    public Task ConfirmAsync(int id) =>
        ChangeStatusAsync(id, ReservationStatus.Active, ReservationStatus.Pending);

    public Task CancelAsync(int id) =>
        ChangeStatusAsync(id, ReservationStatus.Cancelled, ReservationStatus.Pending, ReservationStatus.Active);

    public Task CompleteAsync(int id) =>
        ChangeStatusAsync(id, ReservationStatus.Completed, ReservationStatus.Active);

    public Task DeleteAsync(int id) => DeleteEntityAsync<Reservation>(id);

    private async Task ChangeStatusAsync(int id, string newStatus, params string[] allowedFrom)
    {
        using var db = CreateDb();
        var reservation = await db.Reservations.FindAsync(id) ?? throw NotFound<Reservation>(id);

        if (!allowedFrom.Contains(reservation.Status))
            throw new InvalidOperationException(
                $"Cannot change reservation from '{reservation.Status}' to '{newStatus}'.");

        reservation.Status = newStatus;
        await SaveAsync(db, nameof(Reservation));
    }

    private static IQueryable<Reservation> WithDetails(IQueryable<Reservation> reservations) =>
        reservations
            .Include(r => r.Book)
            .Include(r => r.User);
}

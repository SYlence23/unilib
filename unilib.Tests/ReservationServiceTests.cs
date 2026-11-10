using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class ReservationServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly ReservationService _reservations;
    private readonly int _userId;

    public ReservationServiceTests()
    {
        _reservations = new ReservationService(_db.Create);

        using var db = _db.Create();
        db.Books.Add(new Book { Isbn = "111", Title = "Clean Code", Category = new Category { Name = "Programming" }, Amount = 1 });
        db.SaveChanges();

        _userId = new AuthService(_db.Create)
            .RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!").GetAwaiter().GetResult().Id;
    }

    public void Dispose() => _db.Dispose();

    private async Task<string> StatusOf(int id) => (await _reservations.GetByIdAsync(id))!.Status;

    [Fact]
    public async Task Reopen_ApprovedRequest_IsPendingAgain()
    {
        var reservation = await _reservations.CreateAsync(_userId, "111");
        await _reservations.ConfirmAsync(reservation.Id);

        await _reservations.ReopenAsync(reservation.Id);

        Assert.Equal(ReservationStatus.Pending, await StatusOf(reservation.Id));
    }

    [Fact]
    public async Task Reopen_CancelledRequest_IsPendingAgain()
    {
        var reservation = await _reservations.CreateAsync(_userId, "111");
        await _reservations.CancelAsync(reservation.Id);

        await _reservations.ReopenAsync(reservation.Id);

        Assert.Equal(ReservationStatus.Pending, await StatusOf(reservation.Id));
    }

    [Fact]
    public async Task Reopen_PendingOrCompletedRequest_Throws()
    {
        var pending = await _reservations.CreateAsync(_userId, "111");
        var completed = await _reservations.CreateAsync(_userId, "111");
        await _reservations.ConfirmAsync(completed.Id);
        await _reservations.CompleteAsync(completed.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _reservations.ReopenAsync(pending.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _reservations.ReopenAsync(completed.Id));
        Assert.Equal(ReservationStatus.Completed, await StatusOf(completed.Id));
    }
}

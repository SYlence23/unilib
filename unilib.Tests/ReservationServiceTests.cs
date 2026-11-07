using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class ReservationServiceTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 3, 10);

    private readonly TestDb _db = new();
    private readonly ReservationService _reservations;
    private readonly int _userId;

    public ReservationServiceTests()
    {
        _reservations = new ReservationService(_db.Create, new FixedTime(Today));
        _db.AddBook("111", title: "Clean Code");
        _userId = _db.AddUser();
    }

    public void Dispose() => _db.Dispose();

    private async Task<string> StatusOf(int id) => (await _reservations.GetByIdAsync(id))!.Status;

    [Fact]
    public async Task Create_MakesPendingRequestDatedToday()
    {
        var reservation = await _reservations.CreateAsync(_userId, "111");

        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Equal(Today, reservation.ReservationDate);
    }

    [Fact]
    public async Task Create_UnknownBookOrUser_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _reservations.CreateAsync(_userId, "999"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _reservations.CreateAsync(9999, "111"));
    }

    public enum Action { Confirm, Cancel, Complete, Reopen }

    // Every status change: from, action, and the resulting status (null = refused).
    public static TheoryData<string, Action, string?> Transitions => new()
    {
        { ReservationStatus.Pending, Action.Confirm, ReservationStatus.Active },
        { ReservationStatus.Pending, Action.Cancel, ReservationStatus.Cancelled },
        { ReservationStatus.Pending, Action.Complete, null },
        { ReservationStatus.Pending, Action.Reopen, null },
        { ReservationStatus.Active, Action.Confirm, null },
        { ReservationStatus.Active, Action.Cancel, ReservationStatus.Cancelled },
        { ReservationStatus.Active, Action.Complete, ReservationStatus.Completed },
        { ReservationStatus.Active, Action.Reopen, ReservationStatus.Pending },
        { ReservationStatus.Cancelled, Action.Confirm, null },
        { ReservationStatus.Cancelled, Action.Cancel, null },
        { ReservationStatus.Cancelled, Action.Complete, null },
        { ReservationStatus.Cancelled, Action.Reopen, ReservationStatus.Pending },
        { ReservationStatus.Completed, Action.Confirm, null },
        { ReservationStatus.Completed, Action.Cancel, null },
        { ReservationStatus.Completed, Action.Complete, null },
        { ReservationStatus.Completed, Action.Reopen, null },
    };

    [Theory]
    [MemberData(nameof(Transitions))]
    public async Task StatusChange_FollowsTheFlow(string from, Action action, string? expected)
    {
        var id = _db.AddReservation(_userId, "111", from, Today).Id;
        Task Run() => action switch
        {
            Action.Confirm => _reservations.ConfirmAsync(id),
            Action.Cancel => _reservations.CancelAsync(id),
            Action.Complete => _reservations.CompleteAsync(id),
            _ => _reservations.ReopenAsync(id)
        };

        if (expected == null)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(Run);
            Assert.Equal(from, await StatusOf(id));
        }
        else
        {
            await Run();
            Assert.Equal(expected, await StatusOf(id));
        }
    }

    [Fact]
    public async Task StatusChange_UnknownReservation_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _reservations.ConfirmAsync(9999));
    }

    [Fact]
    public async Task GetPending_ReturnsOnlyPendingOldestFirst()
    {
        var newer = _db.AddReservation(_userId, "111", ReservationStatus.Pending, Today);
        var older = _db.AddReservation(_userId, "111", ReservationStatus.Pending, Today.AddDays(-2));
        _db.AddReservation(_userId, "111", ReservationStatus.Active, Today.AddDays(-5));

        var pending = await _reservations.GetPendingAsync();

        Assert.Equal(new[] { older.Id, newer.Id }, pending.Select(r => r.Id));
        Assert.All(pending, r => Assert.Equal("Clean Code", r.Book.Title));
    }
}

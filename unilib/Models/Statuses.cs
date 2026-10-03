namespace unilib.Models;

public static class RoleNames
{
    public const string Admin = "admin";
    public const string Librarian = "librarian";
    public const string Student = "student";
}

// активна, повернена, прострочено
public static class LoanStatus
{
    public const string Active = "active";
    public const string Returned = "returned";
    public const string Overdue = "overdue";
}

// очікує підтвердження бібліотекаря, активна, скасовано, завершено
public static class ReservationStatus
{
    public const string Pending = "pending";
    public const string Active = "active";
    public const string Cancelled = "cancelled";
    public const string Completed = "completed";
}

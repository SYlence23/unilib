namespace unilib.Services;

// Library rules. LoanService enforces the loan limit; ReservationService enforces the reservation
// limit (CatalogData checks it too, to answer without a round trip to the database).
public static class LibraryRules
{
    public const int MaxOpenReservations = 3;
    public const int MaxActiveLoans = 5;
    public const int LoanPeriodDays = 14;
    public const int RenewalsPerLoan = 2;
}

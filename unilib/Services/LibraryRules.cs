namespace unilib.Services;

// Library rules. LoanService enforces the loan limit; CatalogData enforces the reservation limit.
public static class LibraryRules
{
    public const int MaxOpenReservations = 3;
    public const int MaxActiveLoans = 5;
    public const int LoanPeriodDays = 14;
    public const int RenewalsPerLoan = 2;
}

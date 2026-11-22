using System.Text.RegularExpressions;

namespace unilib.Services;

// Input checks shared by the services. Forms check the same things first to show the error
// next to the field, but the services check again, because they are the last stop before
// the database. Length limits match the column sizes in LibraryDbContext.
// Each check returns the cleaned value (trimmed, emails in lower case) or throws ServiceException.
public static partial class Validation
{
    public const int MinPasswordLength = 8;

    // Hashing is deliberately slow, so very long input would only waste time.
    public const int MaxPasswordLength = 128;

    public const int NameLength = 50;
    public const int EmailLength = 100;
    public const int StudentIdLength = 20;
    public const int PhoneLength = 20;
    public const int IsbnLength = 20;
    public const int TitleLength = 200;
    public const int CategoryLength = 100;
    public const int LibraryNameLength = 150;
    public const int LanguageLength = 50;

    public static string Required(string? value, string field, int maxLength)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0)
            throw new ServiceException($"{field} is required.");
        return MaxLength(text, field, maxLength);
    }

    // Blank input becomes null.
    public static string? Optional(string? value, string field, int maxLength)
    {
        var text = value?.Trim() ?? "";
        return text.Length == 0 ? null : MaxLength(text, field, maxLength);
    }

    public static string Email(string? value)
    {
        var email = Required(value, "Email", EmailLength).ToLowerInvariant();
        if (!EmailPattern().IsMatch(email))
            throw new ServiceException("Enter a valid email address.");
        return email;
    }

    public static void Password(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            throw new ServiceException($"Password must be at least {MinPasswordLength} characters.");
        if (password.Length > MaxPasswordLength)
            throw new ServiceException($"Password must be at most {MaxPasswordLength} characters.");
    }

    public static string? Phone(string? value)
    {
        var phone = Optional(value, "Phone", PhoneLength);
        if (phone != null && !PhonePattern().IsMatch(phone))
            throw new ServiceException("Phone may contain only digits, spaces, +, - and brackets.");
        return phone;
    }

    // Digits, X (the ISBN-10 check digit) and hyphens. The form also checks the 10 or 13 digits.
    public static string Isbn(string? value)
    {
        var isbn = Required(value, "ISBN", IsbnLength).ToUpperInvariant();
        if (!IsbnPattern().IsMatch(isbn))
            throw new ServiceException("ISBN may contain only digits, X and hyphens.");
        return isbn;
    }

    private static string MaxLength(string text, string field, int maxLength) =>
        text.Length > maxLength
            ? throw new ServiceException($"{field} must be at most {maxLength} characters.")
            : text;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[0-9+()\- ]+$")]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"^[0-9X-]+$")]
    private static partial Regex IsbnPattern();
}

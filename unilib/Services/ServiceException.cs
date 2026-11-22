namespace unilib.Services;

// A refused or failed operation whose message is written for the user, such as
// "Loan limit reached". Any other exception is a bug or an infrastructure problem:
// the UI logs it and shows a generic message instead of its text.
public class ServiceException : InvalidOperationException
{
    public ServiceException(string message)
        : base(message)
    {
    }

    public ServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

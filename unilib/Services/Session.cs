using unilib.Models;

namespace unilib.Services;

// The user logged in to this app instance.
public static class Session
{
    public static User? CurrentUser { get; set; }

    public static bool IsLoggedIn => CurrentUser != null;

    public static bool IsInRole(string roleName) => CurrentUser?.Role?.Name == roleName;

    public static void Logout() => CurrentUser = null;
}

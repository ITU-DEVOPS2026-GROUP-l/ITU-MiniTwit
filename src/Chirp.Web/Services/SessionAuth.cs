namespace Chirp.Web.Services;

public static class SessionAuth
{
    public const string UserIdKey = "auth.userId";

    public static string? GetUserId(ISession session)
    {
        return session.GetString(UserIdKey);
    }

    public static void SignIn(ISession session, string userId)
    {
        session.SetString(UserIdKey, userId);
    }

    public static void SignOut(ISession session)
    {
        session.Remove(UserIdKey);
    }
}
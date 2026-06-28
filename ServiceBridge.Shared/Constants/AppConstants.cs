namespace ServiceBridge.Shared.Constants;

public static class AppConstants
{
    public static class Authentication
    {
        public const string CookieScheme = "Cookies";
        public const string JwtScheme = "Bearer";
        public const string AccessTokenTokenName = "access_token";
    }

    public static class Api
    {
        public const string HttpClientName = "ServiceBridgeApi";
    }

    public static class Roles
    {
        public const string User = "User";
        public const string Agent = "Agent";
        public const string Admin = "Admin";
    }

    public static class ErrorCodes
    {
        public const string ValidationFailed = "ValidationFailed";
        public const string DuplicateEmail = "DuplicateEmail";
        public const string InvalidCredentials = "InvalidCredentials";
        public const string Unauthorized = "Unauthorized";
        public const string Forbidden = "Forbidden";
        public const string UserNotFound = "UserNotFound";
        public const string UserInactive = "UserInactive";
        public const string NotFound = "NotFound";
        public const string Conflict = "Conflict";
        public const string UnexpectedError = "UnexpectedError";
    }
}

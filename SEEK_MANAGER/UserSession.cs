namespace SEEK_MANAGER
{
    // Simple in-memory session for the currently authenticated user
    public static class UserSession
    {
        public static int? UserId { get; set; }
        public static string? Username { get; set; }
        public static string? FullName { get; set; }
        public static string? Role { get; set; }

        public static bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}

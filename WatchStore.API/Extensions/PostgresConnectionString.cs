using Npgsql;

namespace WatchStore.API.Extensions
{
    public static class PostgresConnectionString
    {
        // Render (וספקים אחרים) נותנים כתובת בפורמט postgresql://user:pass@host:port/db,
        // ש-Npgsql לא יודע לקרוא. מחרוזת רגילה (Host=...;) מוחזרת כמו שהיא
        public static string Normalize(string connectionString)
        {
            if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return connectionString;
            }

            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':', 2);

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
                Database = uri.AbsolutePath.TrimStart('/'),
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
                // כתובת חיצונית של Render דורשת SSL. בכתובת הפנימית Prefer פשוט ממשיך בלי
                SslMode = SslMode.Prefer
            };

            return builder.ConnectionString;
        }
    }
}

using WatchStore.Core.Interfaces;

namespace WatchStore.Service.Security
{
    // BCrypt מוסיף salt אקראי לכל סיסמה ושומר אותו בתוך ה-hash עצמו
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Verify(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}

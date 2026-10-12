using WatchStore.Core.Entities;

namespace WatchStore.Core.Interfaces
{
    // יצירת JWT - המימוש יושב ב-API, כי שם נמצאות הגדרות האבטחה
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) CreateToken(User user);
    }
}

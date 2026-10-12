using WatchStore.Core.DTOs;

namespace WatchStore.Core.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default);

        // מחזיר null אם שם המשתמש או הסיסמה שגויים
        Task<LoginResponse?> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default);
    }
}

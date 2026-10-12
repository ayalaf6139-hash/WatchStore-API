using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Enums;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;

namespace WatchStore.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<LoginResponse> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            if (await _userRepository.UsernameExistsAsync(
                    request.Username,
                    cancellationToken))
            {
                throw new ConflictException(
                    "שם המשתמש כבר תפוס.");
            }

            var store = new Store
            {
                Name = request.StoreName,
                ContactName = request.ContactName,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                IsActive = true
            };

            // משתמש חדש שנרשם הוא תמיד בעל חנות - Admin נוצר רק דרך ה-seed
            var user = new User
            {
                Username = request.Username,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Role = UserRole.Store,
                Store = store
            };

            // החנות והמשתמש נשמרים יחד ב-SaveChanges אחד = טרנזקציה אחת
            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return BuildLoginResponse(user);
        }

        public async Task<LoginResponse?> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUsernameAsync(
                request.Username,
                cancellationToken);

            // אותה תשובה למשתמש שלא קיים ולסיסמה שגויה,
            // כדי לא לחשוף אילו שמות משתמש קיימים במערכת
            if (user == null ||
                !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return null;
            }

            return BuildLoginResponse(user);
        }

        private LoginResponse BuildLoginResponse(User user)
        {
            var (token, expiresAt) = _tokenService.CreateToken(user);

            return new LoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                Username = user.Username,
                Role = user.Role.ToString(),
                StoreId = user.StoreId
            };
        }
    }
}

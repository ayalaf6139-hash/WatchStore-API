using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStore.Core.DTOs;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // הרשמה של חנות חדשה + משתמש בתפקיד Store
        [HttpPost("register")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<LoginResponse>> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);

            // לא כותבים ללוג את תוכן הבקשה - רק את העובדה שנוצרה חנות
            _logger.LogInformation("New store registered: {StoreId}", response.StoreId);

            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _authService.LoginAsync(request, cancellationToken);

            if (response == null)
            {
                _logger.LogWarning("Failed login attempt");

                return Unauthorized(new
                {
                    status = 401,
                    message = "שם משתמש או סיסמה שגויים.",
                    correlationId = HttpContext.TraceIdentifier
                });
            }

            _logger.LogInformation(
                "User {Username} logged in as {Role}",
                response.Username,
                response.Role);

            return Ok(response);
        }
    }
}

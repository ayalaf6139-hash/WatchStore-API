using System.Security.Claims;
using WatchStore.Core.Interfaces;

namespace WatchStore.API.Security
{
    public class HttpContextCurrentActor : ICurrentActor
    {
        public const string SystemActor = "system";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextCurrentActor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string ActorId
        {
            get
            {
                var userId = _httpContextAccessor.HttpContext?.User
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                return string.IsNullOrEmpty(userId) ? SystemActor : $"user:{userId}";
            }
        }
    }
}

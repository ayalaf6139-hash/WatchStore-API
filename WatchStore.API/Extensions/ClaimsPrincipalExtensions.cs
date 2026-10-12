using System.Security.Claims;
using WatchStore.API.Security;
using WatchStore.Core.Enums;

namespace WatchStore.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsAdmin(this ClaimsPrincipal user) =>
            user.IsInRole(nameof(UserRole.Admin));

        public static int? GetStoreId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(TokenService.StoreIdClaim);

            return int.TryParse(value, out var storeId) ? storeId : null;
        }
    }
}

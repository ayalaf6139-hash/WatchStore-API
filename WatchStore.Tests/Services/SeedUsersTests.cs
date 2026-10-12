using WatchStore.Service.Security;

namespace WatchStore.Tests.Services
{
    // מוודא שה-hash-ים שב-seed באמת תואמים לסיסמאות הדמו שמופיעות ב-README
    public class SeedUsersTests
    {
        private readonly BCryptPasswordHasher _hasher = new();

        [Theory]
        [InlineData("Admin123!", "$2a$11$lfcQS78hRc1tJvdwcOEGpO2LKjJQOs0ubAPCe8a.FBLa0/L7LHtWi")]
        [InlineData("Store123!", "$2a$11$z3wgO5Eb7LnRVWdxMOmFvONVpZCsfgw/OUiq2l0rcUEotW.q7wksC")]
        public void SeedPasswordHash_MatchesDocumentedDemoPassword(string password, string hash)
        {
            Assert.True(_hasher.Verify(password, hash));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WatchStore.Core.Enums;

namespace WatchStore.Core.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public int? StoreId { get; set; }

        // Admin לא משויך לחנות, ולכן הקשר אופציונלי
        public Store? Store { get; set; }
    }
}

namespace WatchStore.Core.Entities
{
    public class Store
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ContactName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        // חנות אחת מבצעת הזמנות רבות
        public ICollection<Order> Orders { get; set; }
            = new List<Order>();
    }
}
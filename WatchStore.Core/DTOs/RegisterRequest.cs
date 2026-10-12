using System.ComponentModel.DataAnnotations;

namespace WatchStore.Core.DTOs
{
    // הרשמה של בעל חנות: יוצרת חנות חדשה + משתמש בתפקיד Store
    public class RegisterRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string StoreName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContactName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;
    }
}

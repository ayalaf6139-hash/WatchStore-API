using System.ComponentModel.DataAnnotations;

namespace WatchStore.Core.DTOs
{
    public class LoginRequest
    {
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }
}

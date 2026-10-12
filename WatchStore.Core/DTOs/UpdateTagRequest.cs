using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace WatchStore.Core.DTOs
{
    public class UpdateTagRequest
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
    }
}
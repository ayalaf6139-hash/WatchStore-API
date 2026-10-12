using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchStore.Core.DTOs
{
    public class ProductTagResponse
    {
        public int ProductId { get; set; }

        public int TagId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string TagName { get; set; } = string.Empty;
    }
}
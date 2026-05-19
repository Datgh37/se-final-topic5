using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace WebUITopic5_Team4.DTOs
{
    public class ProductUpdateInfoDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ProductSlug { get; set; }
        public int CategoryId { get; set; }
        public int? SeriesId { get; set; }
        public string SupplierId { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public string? Description { get; set; }
        public double Discount { get; set; }
        public int StockQuantity { get; set; }

        public IFormFile? PrimaryImageFile { get; set; }
        public List<IFormFile>? SubImageFiles { get; set; }
    }
}

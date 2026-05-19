namespace WebUITopic5_Team4.Models.ViewModels
{
    public class ProductCardViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public decimal FinalPrice => UnitPrice;
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? PrimaryImage { get; set; }
        public int StockQuantity { get; set; }
        public bool IsOnSale => false;
        public bool IsInStock => StockQuantity > 0;
        public bool IsFavorite { get; set; }
    }
}

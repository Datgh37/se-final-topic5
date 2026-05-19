namespace WebUITopic5_Team4.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho Cart Preview dropdown trên Header
    /// </summary>
    public class CartPreviewViewModel
    {
        public int TotalItems { get; set; }
        public decimal GrandTotal { get; set; }
        public List<CartPreviewItem> Items { get; set; } = new();
    }

    public class CartPreviewItem
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }
}

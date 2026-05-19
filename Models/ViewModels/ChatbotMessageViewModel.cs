using System.Collections.Generic;

namespace WebUITopic5_Team4.Models.ViewModels
{
    public class ChatbotRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatbotResponse
    {
        public string Reply { get; set; } = string.Empty;
        public List<ChatbotProductViewModel> Products { get; set; } = new List<ChatbotProductViewModel>();
    }

    public class ChatbotProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public double Discount { get; set; }
        public decimal FinalPrice { get; set; }
        public int StockQuantity { get; set; }
        public string PrimaryImageUrl { get; set; } = string.Empty;
    }
}

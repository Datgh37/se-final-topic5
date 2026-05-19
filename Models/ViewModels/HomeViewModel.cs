using WebUITopic5_Team4.Models.ViewModels;
using WebUITopic5_Team4.Models;

namespace WebUITopic5_Team4.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<ProductCardViewModel> FeaturedProducts { get; set; } = new();
        public List<Category> FeaturedCategories { get; set; } = new();
    }
}

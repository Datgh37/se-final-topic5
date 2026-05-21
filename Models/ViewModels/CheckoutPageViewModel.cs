using WebUITopic5_Team4.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebUITopic5_Team4.Models.ViewModels
{
    public class CheckoutPageViewModel
    {
        [ValidateNever]
        public Cart Cart { get; set; }

        public CheckOutViewModel Checkout { get; set; }
    }
}

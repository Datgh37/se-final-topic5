using System.ComponentModel.DataAnnotations;

namespace WebUITopic5_Team4.Models.ViewModels
{
    public class CheckOutViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Province { get; set; }

        [Required]
        public string Address { get; set; }

        public string Note { get; set; }

        public string PaymentMethod { get; set; }
    }
}

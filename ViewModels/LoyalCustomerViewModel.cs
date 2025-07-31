namespace PawVerse.ViewModels
{
    public class LoyalCustomerViewModel
    {
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; } // Optional, for more info
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; } // Optional, for more detailed insights
        public string? AvatarUrl { get; set; } // Optional, if users have avatars
    }
}

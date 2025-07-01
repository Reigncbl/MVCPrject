namespace MVCPrject.Models
{
    public class ProfileViewModel
    {
        public User User { get; set; } = new User();
        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int RecipeCount { get; set; }
        public List<Recipe> RecentRecipes { get; set; } = new List<Recipe>();
    }
}
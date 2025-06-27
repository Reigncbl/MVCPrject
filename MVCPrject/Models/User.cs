using Microsoft.AspNetCore.Identity;

namespace MVCPrject.Models
{
    public class User : IdentityUser
    {
        public string? Name { get; set; }
    }
    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
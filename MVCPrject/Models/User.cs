using Microsoft.AspNetCore.Identity;

namespace MVCPrject.Models
{
    public class User : IdentityUser
    {
        public string? Name { get; set; }
    }
}
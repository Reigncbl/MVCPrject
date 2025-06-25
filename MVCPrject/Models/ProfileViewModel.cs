using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCPrject.Models
{

    // Example Post class, adjust as needed
     [Table("UserPosting")]
    public class UserContent
    {
        [Key]
        public int PostID { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? UserId { get; set; }
        public User? User { get; set; }
    }
       
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCPrject.Models
{
    public class Follows
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string FollowerId { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string FolloweeId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

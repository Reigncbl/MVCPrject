using System.ComponentModel.DataAnnotations;

namespace MVCPrject.Models
{
    public class Chat
    {
        [StringLength(2000)]
        public string UserInput { get; set; } = "";

        public string AiResponse { get; set; } = "";
        public List<string> History { get; set; } = new();
    }
}
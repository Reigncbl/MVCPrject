using System.ComponentModel.DataAnnotations;

namespace MVCPrject.Models
{
    public class Chat
    {
        [MaxLength(100000)]
        public string UserInput { get; set; } = "";
        public string AiResponse { get; set; } = "";
        public List<string> History { get; set; } = new();
    }

}

namespace MVCPrject.Models
{
    public class Chat
    {
        public string UserInput { get; set; }
        public string AiResponse { get; set; }
        public List<string> History { get; set; } = new();
    }

}

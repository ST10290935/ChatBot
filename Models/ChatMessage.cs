namespace MentalHealthSupportApp.Models
{
    public class ChatMessage
    {
        public string Sender { get; set; }  // "User" or "AI"
        public string Text { get; set; }
    }

    public class UserMessage
    {
        public string Text { get; set; }
    }
}

namespace Api.Configuration
{
    public class CorsPolicyConfig
    {
        public string Origin { get; set; } = string.Empty;
        public List<string> Methods { get; set; } = new();
        public List<string> Headers { get; set; } = new();
    }
}

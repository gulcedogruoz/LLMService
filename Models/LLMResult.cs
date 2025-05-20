public class LLMResult
{
    public string Intent { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Response { get; set; } 
}

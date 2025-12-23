namespace ChatApi.Models;

public record ChatMessage
{
    public string User { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
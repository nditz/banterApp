namespace BanterApp.Api.Data.Entities;

public class ReceiptStoryCandidate
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public string StoryType { get; set; } = string.Empty;
    public int Rank { get; set; }
    public string Summary { get; set; } = string.Empty;

    public PredictionReceipt Receipt { get; set; } = null!;
}

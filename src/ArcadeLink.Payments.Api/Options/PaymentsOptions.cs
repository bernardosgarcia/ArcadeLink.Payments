namespace ArcadeLink.Payments.Api.Options;

public class PaymentsOptions
{
    public const string SectionName = "Payments";

    public double ApprovalRate { get; set; } = 0.8;
}

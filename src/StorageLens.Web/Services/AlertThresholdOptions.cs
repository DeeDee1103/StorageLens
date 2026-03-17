namespace StorageLens.Web.Services;

public sealed class AlertThresholdOptions
{
    public const string SectionName = "Alerts";

    public int MinThresholdPercent { get; set; } = 70;
    public int MaxThresholdPercent { get; set; } = 90;
    public string RecipientEmail { get; set; } = "dddevacct16783@gmail.com";
}

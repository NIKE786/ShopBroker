namespace ShopBroker.Helpers;

public static class PolicyHelper
{
    public static readonly string[] Types = { "Life", "Health", "Motor", "Property", "Travel" };
    public static readonly string[] Frequencies = { "Monthly", "Quarterly", "HalfYearly", "Yearly" };
    public static readonly string[] Statuses = { "Active", "Lapsed", "Expired", "Claimed", "Cancelled" };
    public static int Months(string? freq) => freq switch { "Monthly" => 1, "Quarterly" => 3, "HalfYearly" => 6, _ => 12 };
}

public static class OrderHelper
{
    public static readonly string[] Statuses =
        { "Placed", "Awaiting Payment", "Payment Link Sent", "Paid", "Processing", "Ready", "Delivered", "Cancelled" };
}

public static class WaHelper
{
    public static string Link(string? phone, string text)
    {
        var d = new string((phone ?? "").Where(char.IsDigit).ToArray());
        if (d.Length == 10) d = "91" + d;
        return $"https://wa.me/{d}?text={Uri.EscapeDataString(text)}";
    }
}

public static class UiHelper
{
    public static string PayLabel(string? m) => m == "COD" ? "Counter / Cash" : m == "Link" ? "Online payment link" : (m ?? "");

    public static string StatusClass(string? s) => s switch
    {
        "Paid" or "Delivered" or "Ready" => "bg-success",
        "Payment Link Sent" or "Processing" => "bg-info text-dark",
        "Cancelled" => "bg-danger",
        "Awaiting Payment" => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    public static string Icon(string? category) => (category ?? "").ToLowerInvariant() switch
    {
        var c when c.Contains("pvc") => "💳",
        var c when c.Contains("frame") => "🖼️",
        var c when c.Contains("photo") => "📷",
        var c when c.Contains("print") => "🖨️",
        var c when c.Contains("gift") => "🎁",
        var c when c.Contains("insur") => "🛡️",
        _ => "🛍️"
    };
}

using System.Text.Json;
namespace ShopBroker.Helpers;

public static class CartHelper
{
    const string Key = "cart";
    public static Dictionary<int, int> Get(ISession s)
    {
        var json = s.GetString(Key);
        return string.IsNullOrEmpty(json)
            ? new Dictionary<int, int>()
            : JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
    }
    public static void Save(ISession s, Dictionary<int, int> cart) => s.SetString(Key, JsonSerializer.Serialize(cart));
    public static void Clear(ISession s) => s.Remove(Key);
    public static int Count(ISession s) => Get(s).Values.Sum();
}

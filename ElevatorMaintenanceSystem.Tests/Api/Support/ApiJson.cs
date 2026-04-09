using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElevatorMaintenanceSystem.Tests.Api.Support;

internal static class ApiJson
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static StringContent Content<T>(T value)
    {
        return new StringContent(
            JsonSerializer.Serialize(value, SerializerOptions),
            Encoding.UTF8,
            "application/json");
    }

    public static async Task<T> ReadAsync<T>(HttpContent content)
    {
        var result = await content.ReadFromJsonAsync<T>(SerializerOptions);
        return result ?? throw new InvalidOperationException($"Expected {typeof(T).Name} payload.");
    }

    public static async Task<List<T>> ReadListAsync<T>(HttpContent content)
    {
        var result = await content.ReadFromJsonAsync<List<T>>(SerializerOptions);
        return result ?? [];
    }
}

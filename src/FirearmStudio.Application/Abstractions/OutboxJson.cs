using System.Text.Json;

namespace FirearmStudio.Application.Abstractions;

internal static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

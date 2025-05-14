using System.Text.Json.Serialization;

namespace HidRecorder.Models;

[JsonSerializable(typeof(HidProject))]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(HidData))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

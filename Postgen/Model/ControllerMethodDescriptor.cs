using System.Text.Json;

namespace Postgen.Model;

internal sealed class ControllerMethodDescriptor
{
    public string Name { get; set; }
    public string HttpMethod { get; set; }
    public string Route { get; set; }
    public JsonDocument? Body { get; set; }
}
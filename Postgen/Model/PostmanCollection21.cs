using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Postgen;

internal sealed class PostmanCollection21
{
    public Collection21Info Info { get; set; }
    
    /// <summary>
    /// Item or item group
    /// </summary>
    public List<object> Item { get; set; }
}

internal sealed class Collection21Info
{
    [JsonPropertyName("_postman_id")]
    public Guid PostmanId { get; set; }

    public string Name { get; set; } = null!;

    public string Schema { get; } = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json";
    
    [JsonPropertyName("_exporter_id")]
    public int ExporterId { get; set; }
}

internal sealed class Collection21ItemGroup
{
    public string Name { get; set; }
    
    /// <summary>
    /// Item or item group
    /// </summary>
    public List<object> Item { get; set; }
}

internal sealed class Collection21Item
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Request Request {get; set; }
}

internal sealed class Request
{
    public string Url { get; set; }
    public string Method { get; set; }
    public RequestBody Body { get; set; }
}

internal sealed class RequestBody
{
    public string Mode { get; set; } = "raw";
    public string Raw { get; set; }
    public RequestBodyOptions Options { get; set; }
}

internal sealed class RequestBodyOptions
{
    public RawBodyOptions Raw { get; set; }
}

internal sealed class RawBodyOptions
{
    public string Language { get; set; }
}
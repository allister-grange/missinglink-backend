using System.Text.Json.Serialization;

[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public class RouteDTO
{
  [JsonPropertyName("id")]
  public int Id { get; set; }

  [JsonPropertyName("route_id")]
  [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
  public string RouteId { get; set; }

  [JsonPropertyName("agency_id")]
  public string AgencyId { get; set; }

  [JsonPropertyName("route_short_name")]
  public string RouteShortName { get; set; }

  [JsonPropertyName("route_long_name")]
  public string RouteLongName { get; set; }

  [JsonPropertyName("route_desc")]
  public string RouteDesc { get; set; }

  [JsonPropertyName("route_type")]
  public int RouteType { get; set; }

  [JsonPropertyName("route_color")]
  public string RouteColor { get; set; }

  [JsonPropertyName("route_text_color")]
  public string RouteTextColor { get; set; }

  [JsonPropertyName("route_url")]
  public string RouteUrl { get; set; }
}


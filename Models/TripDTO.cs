using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace missinglink.Models
{
  public class TripDTO
  {
    [JsonPropertyName("route_id")]
    public int RouteId { get; set; }

    [JsonPropertyName("service_id")]
    public string ServiceId { get; set; }

    [JsonPropertyName("trip_id")]
    public string TripId { get; set; }

    [JsonPropertyName("trip_headsign")]
    public string TripHeadsign { get; set; }

    [JsonPropertyName("direction_id")]
    public int DirectionId { get; set; }

    [JsonPropertyName("block_id")]
    public string BlockId { get; set; }

    [JsonPropertyName("shape_id")]
    public string ShapeId { get; set; }

    [JsonPropertyName("wheelchair_accessible")]
    public int WheelchairAccessible { get; set; }

    [JsonPropertyName("bikes_allowed")]
    public int BikesAllowed { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }
  }
}
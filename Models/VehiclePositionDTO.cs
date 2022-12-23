using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace missinglink.Models.VehiclePositions
{

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class Header
  {
    [JsonPropertyName("gtfsRealtimeVersion")]
    public string GtfsRealtimeVersion { get; set; }

    [JsonPropertyName("incrementality")]
    public int Incrementality { get; set; }

    [JsonPropertyName("timestamp")]
    public int Timestamp { get; set; }
  }

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class Trip
  {
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; }

    [JsonPropertyName("trip_id")]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public string TripId { get; set; }

    [JsonPropertyName("direction_id")]
    public int DirectionId { get; set; }

    [JsonPropertyName("route_id")]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public string RouteId { get; set; }

    [JsonPropertyName("schedule_relationship")]
    public int ScheduleRelationship { get; set; }

    [JsonPropertyName("start_date")]
    public string StartDate { get; set; }
  }

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class Position
  {
    [JsonPropertyName("bearing")]
    public double Bearing { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
  }

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class VehiclePostionId
  {
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public string Id { get; set; }
  }

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class VehiclePosition
  {
    // [JsonPropertyName("id")]
    // public string Id { get; set; }

    [JsonPropertyName("trip")]
    public Trip Trip { get; set; }

    [JsonPropertyName("position")]
    public Position Position { get; set; }

    [JsonPropertyName("vehicle")]
    public VehiclePostionId Vehicle { get; set; }

    [JsonPropertyName("timestamp")]
    public int Timestamp { get; set; }
  }

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class VehiclePositionHolder
  {
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public string Id { get; set; }

    [JsonPropertyName("vehicle")]
    public VehiclePosition VehiclePosition { get; set; }
  }

  public class VehiclePostionDTO
  {
    [JsonPropertyName("header")]
    public Header Header { get; set; }

    [JsonPropertyName("entity")]
    public List<VehiclePositionHolder> VehiclePositions { get; set; }
  }

}
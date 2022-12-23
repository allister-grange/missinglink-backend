// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse)  {get; set;}
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace missinglink.Models
{

  public class Header
  {
    [JsonPropertyName("gtfsRealtimeVersion")]
    public string GtfsRealtimeVersion { get; set; }

    [JsonPropertyName("incrementality")]
    public int Incrementality { get; set; }

    [JsonPropertyName("timestamp")]
    public int Timestamp { get; set; }
  }

  public class Arrival
  {
    [JsonPropertyName("delay")]
    public int Delay { get; set; }

    [JsonPropertyName("time")]
    public int Time { get; set; }
  }

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class StopTimeUpdate
  {
    [JsonPropertyName("schedule_relationship")]
    public int ScheduleRelationship { get; set; }

    [JsonPropertyName("stop_sequence")]
    public int StopSequence { get; set; }

    [JsonPropertyName("arrival")]
    public Arrival Arrival { get; set; }

    [JsonPropertyName("stop_id")]
    public int StopId { get; set; }
  }

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class Trip
  {
    [JsonPropertyName("schedule_relationship")]
    public int ScheduleRelationship { get; set; }

    [JsonPropertyName("start_time")]
    public string StartTime { get; set; }

    [JsonPropertyName("trip_id")]
    public string TripId { get; set; }
  }

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class Vehicle
  {
    [JsonPropertyName("id")]
    public string Id { get; set; }
  }
  public class TripUpdate
  {
    [JsonPropertyName("stop_time_update")]
    public StopTimeUpdate StopTimeUpdate { get; set; }

    [JsonPropertyName("trip")]
    public Trip Trip { get; set; }

    [JsonPropertyName("vehicle")]
    public Vehicle Vehicle { get; set; }

    [JsonPropertyName("timestamp")]
    public int Timestamp { get; set; }
  }

  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class TripUpdateHolder
  {
    [JsonPropertyName("trip_update")]
    public TripUpdate TripUpdate { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }
  }

  public class BusTripDTO
  {
    [JsonPropertyName("header")]
    public Header Header { get; set; }

    [JsonPropertyName("entity")]
    public List<TripUpdateHolder> Trips { get; set; }
  }

}
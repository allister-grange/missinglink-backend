using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace missinglink.Models
{
  [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
  public class Bus
  {

    [Key]
    [JsonPropertyName("vehicle_id")]
    public string VehicleId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
    public int StopId { get; set; }
    public int Delay { get; set; }
    public int RouteId { get; set; }
    public string TripId { get; set; }
    public string RouteDescription { get; set; }
    public string RouteShortName { get; set; }
    public string RouteLongName { get; set; }
    public double Lat { get; set; }
    public double Long { get; set; }
    public int Bearing { get; set; }
  }
}
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace missinglink.Models
{
  public class MetlinkService
  {

    [Key]
    [JsonProperty("vehicle_id")]
    public string VehicleId { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
    public string StopId { get; set; }
    public int Delay { get; set; }
    public string RouteId { get; set; }
    public string TripId { get; set; }
    public string RouteDescription { get; set; }
    public string RouteShortName { get; set; }
    public string RouteLongName { get; set; }
    public double Lat { get; set; }
    public double Long { get; set; }
    public double Bearing { get; set; }
  }
}
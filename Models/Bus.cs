using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace missinglink.Models {
  public class Bus {
    
    [Key]
    [JsonPropertyName("vehicle_id")]
    public string VehicleId {get; set;}

    [JsonPropertyName("status")]
    public string Status {get; set;}
    public string StopId {get; set;}
    public int Delay {get; set;}
    public string RouteId {get; set;}
    public string TripId {get; set;}
    public string RouteDescription {get; set;}
    public string RouteShortName {get; set;}
    public string RouteLongName {get; set;}
    public double Lat {get; set;}
    public double Long {get; set;}
    public int Bearing {get; set;}
  }
}
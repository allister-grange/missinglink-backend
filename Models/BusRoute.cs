using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace missinglink.Models {
  public class BusRoute {

    [JsonPropertyName("departures")]
    public IEnumerable<Bus> Departures {get; set;}
  }

}


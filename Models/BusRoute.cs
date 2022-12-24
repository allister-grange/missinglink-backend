using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace missinglink.Models
{
  public class BusRoute
  {

    [JsonProperty("departures")]
    public IEnumerable<Bus> Departures { get; set; }
  }

}


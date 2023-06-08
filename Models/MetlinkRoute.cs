using System.Collections.Generic;
using Newtonsoft.Json;

namespace missinglink.Models
{
  public class MetlinkRoute
  {

    [JsonProperty("departures")]
    public IEnumerable<MetlinkService> Departures { get; set; }
  }

}


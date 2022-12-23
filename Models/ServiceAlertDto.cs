using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class ServiceAlertDto
{

  public Header header { get; set; }
  public List<Entity> entity { get; set; }

}

public class Header
{
  public string gtfs_realtime_version { get; set; }
  public int timestamp { get; set; }
  public int incrementality { get; set; }
}

public class ActivePeriod
{
  public int start { get; set; }
  public int end { get; set; }
}

public class Translation
{
  public string language { get; set; }
  public string text { get; set; }
}

public class DescriptionText
{
  public List<Translation> translation { get; set; }
}

public class HeaderText
{
  public List<Translation> translation { get; set; }
}

[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public class ServiceAlertTrip
{
  public int trip_id { get; set; }
}

[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public class InformedEntity
{
  public int route_id { get; set; }
  public int route_type { get; set; }
  public int stop_id { get; set; }
  public ServiceAlertTrip trip { get; set; }
}

public class Url
{
  public List<Translation> translation { get; set; }
}

public class Alert
{
  public List<ActivePeriod> active_period { get; set; }
  public string effect { get; set; }
  public string cause { get; set; }
  public DescriptionText description_text { get; set; }
  public HeaderText header_text { get; set; }
  public List<InformedEntity> informed_entity { get; set; }
  public string severity_level { get; set; }
  public Url url { get; set; }
}

public class Entity
{
  public Alert alert { get; set; }
  public string id { get; set; }
}

public class Root
{
  public Header header { get; set; }
  public List<Entity> entity { get; set; }
}
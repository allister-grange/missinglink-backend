using System;
using System.ComponentModel.DataAnnotations;

namespace missinglink.Models
{
  public class BusStatistic
  {

    [Key]
    public int BatchId { get; set; }

    public int DelayedBuses { get; set; }

    public int TotalBuses { get; set; }

    public int CancelledBuses { get; set; }

    public int EarlyBuses { get; set; }

    public int OnTimeBuses { get; set; }

    public int NotReportingTimeBuses { get; set; }

    public DateTime Timestamp { get; set; }
  }
}
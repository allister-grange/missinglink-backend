using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using missinglink.Contexts;
using missinglink.Models;
using missinglink.Services;

namespace missinglink.Controllers
{
  [ApiController]
  [Route("api/v1")]
  public class BusController : ControllerBase
  {
    private readonly ILogger<BusController> _logger;
    private readonly BusContext _BusContext;
    private readonly MetlinkAPIServices _MetlinkAPIService;
    public BusController(ILogger<BusController> logger, BusContext BusContext,
      MetlinkAPIServices MetlinkAPIService)
    {
      _logger = logger;
      _BusContext = BusContext;
      _MetlinkAPIService = MetlinkAPIService;
    }

    [HttpGet("updates")]
    public async Task<IEnumerable<Bus>> BusTripUpdates()
    {
      var buses = _BusContext.Buses;

      if (buses == null || buses.Count() == 0)
      {
        throw new Exception("Buses table in database not populated. Try calling GetBusTripsFromTripUpdates.");
      }

      return buses;
    }

    [HttpGet("statistics")]
    public async Task<IEnumerable<BusStatistic>> GetBusStatistics(string? startDate, string? endDate)
    {

      _logger.LogInformation("passed in dates: " + startDate + " " + endDate);
      IEnumerable<BusStatistic> stats = null;

      if (!String.IsNullOrEmpty(startDate) && !String.IsNullOrEmpty(endDate))
      {
        DateTime startDateInput;
        DateTime endDateInput;

        try
        {
          startDateInput = DateTime.Parse(startDate);
          endDateInput = DateTime.Parse(endDate);

          stats = _BusContext.BusStatistic.Where((stat) => (
            stat.Timestamp >= startDateInput && stat.Timestamp <= endDateInput
          ));
        }
        catch (System.FormatException e)
        {
          _logger.LogError($"Your date inputs were formatted incorrectly {e.ToString()}");
          throw new ArgumentException("Your date inputs were formatted incorrectly");
        }
        _logger.LogInformation("Parsed dates: " + startDateInput + " " + endDateInput);
      }
      else
      {
        stats = _BusContext.BusStatistic;
      }

      if (stats == null || stats.Count() == 0)
      {
        throw new Exception("BusStatistic table in database not populated. Try calling UpdateBusStatistics.");
      }

      return stats;
    }

    [HttpPost("updates")]
    public async Task<ActionResult> UpdateBusTrips()
    {
      try
      {
        var allBuses = await _MetlinkAPIService.GetBusUpdates();
        if (allBuses.Count > 0)
        {
          UpdateDbWithNewBuses(allBuses);
          return Ok();
        }
        else
        {
          return NotFound();
        }
      }
      catch
      {
        return Problem("Was unable to query Metlink's API");
      }
    }


    [HttpPost("statistics")]
    public async Task<ActionResult> UpdateBusStatistics()
    {
      await UpdateBusTrips();

      var allBuses = await BusTripUpdates();
      var newBusStatistic = new BusStatistic();

      if (allBuses == null)
      {
        return NotFound("The bus table must be empty");
      }

      newBusStatistic.DelayedBuses = allBuses.Where(bus => bus.Status == "LATE").Count();
      newBusStatistic.EarlyBuses = allBuses.Where(bus => bus.Status == "EARLY").Count();
      newBusStatistic.NotReportingTimeBuses = allBuses.Where(bus => bus.Status == "UNKNOWN").Count();
      newBusStatistic.OnTimeBuses = allBuses.Where(bus => bus.Status == "ONTIME").Count();
      newBusStatistic.CancelledBuses = allBuses.Where(bus => bus.Status == "CANCELLED").Count();
      newBusStatistic.TotalBuses = allBuses.Count();

      DateTime utcTime = DateTime.UtcNow;
      TimeZoneInfo serverZone = TimeZoneInfo.FindSystemTimeZoneById("NZ");
      DateTime currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, serverZone);
      newBusStatistic.Timestamp = currentDateTime;

      await _BusContext.BusStatistic.AddAsync(newBusStatistic);
      await _BusContext.SaveChangesAsync();
      return Ok();
    }

    // clears all buses from the db and starts again with the fresh data
    private void UpdateDbWithNewBuses(List<Bus> buses)
    {
      _BusContext.Buses.RemoveRange(_BusContext.Buses);
      _BusContext.Buses.AddRange(buses);
      _BusContext.SaveChanges();
    }
  }
}

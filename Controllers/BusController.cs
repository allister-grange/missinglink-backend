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

    [HttpGet("statstics")]
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

      var tripUpdatesTask = _MetlinkAPIService.GetTripUpdates();
      var tripsTask = _MetlinkAPIService.GetTrips();
      var routesTask = _MetlinkAPIService.GetRoutes();
      var positionsTask = _MetlinkAPIService.GetVehiclePositions();
      var serviceAlertTask = _MetlinkAPIService.GetCancelledBusesFromMetlink();

      await Task.WhenAll(tripUpdatesTask, tripsTask, routesTask, positionsTask, serviceAlertTask);

      var tripUpdates = await tripUpdatesTask;
      var trips = await tripsTask;
      var routes = await routesTask;
      var positions = await positionsTask;
      var serviceAlert = await serviceAlertTask;

      var allBuses = new List<Bus>();

      if (tripUpdates.Count > 0)
      {
        _logger.LogInformation("Parsing buses from trip updates...");
        allBuses = ParseBusesFromTripUpdates(tripUpdates);
        _logger.LogInformation("Finished parsing bus trips");
      }
      else
      {
        return NotFound();
      }

      // match the route_id from /trips to the route_id in /routes and then set the 
      // short name and description to the bus with the matching "trip_id" from /trip_updates
      // tripUpdate (trip_id)
      // trip (route_id, trip_id)
      // routes (route_id)

      allBuses.ForEach(bus =>
      {
        var tripThatBusIsOn = trips.Find(trip => trip.TripId == bus.TripId);
        var positionForBus = positions.Find(pos => pos.VehiclePosition.Vehicle.Id == bus.VehicleId);

        if (tripThatBusIsOn == null)
        {
          _logger.LogWarning("No Trip found for bus " + bus.VehicleId);
          return;
        }
        var routeThatBusIsOn = routes.Find(route => route.RouteId == tripThatBusIsOn.RouteId.ToString());
        if (routeThatBusIsOn != null)
        {
          bus.RouteId = routeThatBusIsOn.RouteId;
          bus.RouteDescription = routeThatBusIsOn.RouteDesc;
          bus.RouteShortName = routeThatBusIsOn.RouteShortName;
          bus.RouteLongName = routeThatBusIsOn.RouteLongName;
        }
        else
        {
          _logger.LogError($"Route that the bus {bus.VehicleId} is on is null");
        }
        if (positionForBus != null)
        {
          bus.Bearing = positionForBus.VehiclePosition.Position.Bearing;
          bus.Lat = positionForBus.VehiclePosition.Position.Latitude;
          bus.Long = positionForBus.VehiclePosition.Position.Longitude;
        }
        else
        {
          _logger.LogError($"Position for bus {bus.VehicleId}");
        }
      });

      serviceAlert.entity.ForEach(entity =>
      {
        var alert = entity.alert.header_text.translation[0].text;
        var routeShortName = routes.Find(route => route.RouteId == entity.alert.informed_entity[0].route_id);

        if(routeShortName == null) {
        
        }

        if (alert.Contains("cancel") && routeShortName.RouteShortName != null)
        {
          allBuses.Add(new Bus()
          {
            Status = "CANCELLED",
            RouteLongName = entity.alert.header_text.translation[0].text,
            RouteShortName = routeShortName.RouteShortName,
            VehicleId = System.Guid.NewGuid().ToString()
          });
        }
      });

      UpdateDbWithNewBuses(allBuses);

      return Ok();
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

    private List<Bus> ParseBusesFromTripUpdates(List<TripUpdateHolder> trips)
    {

      List<Bus> allBuses = new List<Bus>();

      trips.ToList().ForEach(trip =>
      {

        if (trip.TripUpdate.Trip.TripId.Contains("RAIL") || trip.TripUpdate.Trip.TripId.Contains("rail"))
        {
          return;
        }

        var bus = new Bus();

        bus.VehicleId = trip.TripUpdate.Vehicle.Id;
        int delay = trip.TripUpdate.StopTimeUpdate.Arrival.Delay;
        if (delay > 120)
        {
          bus.Status = "LATE";
        }
        else if (delay < -90)
        {
          bus.Status = "EARLY";
        }
        else if (delay == 0)
        {
          bus.Status = "UNKNOWN";
        }
        else
        {
          bus.Status = "ONTIME";
        }
        bus.TripId = trip.TripUpdate.Trip.TripId;
        bus.StopId = trip.TripUpdate.StopTimeUpdate.StopId;
        bus.Delay = trip.TripUpdate.StopTimeUpdate.Arrival.Delay;
        if (allBuses.Find(toFind => bus.VehicleId == toFind.VehicleId) == null)
        {
          allBuses.Add(bus);
        }
      });

      return allBuses;
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

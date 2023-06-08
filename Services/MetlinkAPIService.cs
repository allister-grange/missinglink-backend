using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using missinglink.Models;
using missinglink.Models.VehiclePositions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace missinglink.Services
{
  public class MetlinkAPIServices
  {

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetlinkAPIServices> _logger;


    public MetlinkAPIServices(ILogger<MetlinkAPIServices> logger, IHttpClientFactory clientFactory, IConfiguration configuration)
    {
      _httpClient = clientFactory.CreateClient("metlinkService");
      _configuration = configuration;
      _logger = logger;
    }

    public async Task<List<MetlinkService>> GetBusUpdates()
    {

      try
      {
        var tripUpdatesTask = GetTripUpdates();
        var tripsTask = GetTrips();
        var routesTask = GetRoutes();
        var positionsTask = GetVehiclePositions();
        var cancelledServicesTask = GetCancelledBusesFromMetlink();

        await Task.WhenAll(tripUpdatesTask, tripsTask, routesTask, positionsTask, cancelledServicesTask);

        var tripUpdates = await tripUpdatesTask;
        var trips = await tripsTask;
        var routes = await routesTask;
        var positions = await positionsTask;
        var cancelledServices = await cancelledServicesTask;

        var allBuses = new List<MetlinkService>();

        if (tripUpdates.Count > 0)
        {
          _logger.LogInformation("Parsing buses from trip updates...");
          allBuses = ParseBusesFromTripUpdates(tripUpdates);
          _logger.LogInformation("Finished parsing bus trips");
        }

        allBuses.ForEach(bus =>
        {
          var tripThatBusIsOn = trips.Find(trip => trip.TripId == bus.TripId);
          var positionForBus = positions.Find(pos => pos.VehiclePosition.Vehicle.Id == bus.VehicleId);
          var routeThatBusIsOn = routes.Find(route => route.RouteId == tripThatBusIsOn.RouteId);

          if (routeThatBusIsOn != null)
          {
            bus.RouteId = routeThatBusIsOn.RouteId;
            bus.RouteDescription = routeThatBusIsOn.RouteDesc;
            bus.RouteShortName = routeThatBusIsOn.RouteShortName;
            bus.RouteLongName = routeThatBusIsOn.RouteLongName;
            if (bus.TripId.Contains("RAIL") || bus.TripId.Contains("rail"))
            {
              Console.WriteLine(bus.TripId);
            }

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
            _logger.LogError($"Position for bus {bus.VehicleId} is null");
          }
        });

        cancelledServices.ForEach(cancellation =>
        {

          var route = routes.Find(route => route.RouteId == cancellation.RouteId);

          if (route == null)
          {
            return;
          }

          allBuses.Add(new MetlinkService()
          {
            Status = "CANCELLED",
            RouteLongName = route.RouteLongName,
            RouteShortName = route.RouteLongName,
            VehicleId = System.Guid.NewGuid().ToString()
          });
        });

        return allBuses;
      }
      catch
      {
        throw;
      }
    }

    private List<MetlinkService> ParseBusesFromTripUpdates(List<TripUpdateHolder> trips)
    {

      List<MetlinkService> allBuses = new List<MetlinkService>();

      trips.ToList().ForEach(trip =>
      {
        var bus = new MetlinkService();

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

    public async Task<IEnumerable<MetlinkService>> GetBusesFromStopId(string stopId)
    {
      try
      {
        var response = await MakeAPIRequest($"https://api.opendata.metlink.org.nz/v1/stop-predictions?stop_id={stopId}");
        MetlinkRoute res = null;

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<MetlinkRoute>(responseStream);
        }
        else
        {
          Console.WriteLine("Error in GetBusesFromStopId");
        }

        return res == null ? Enumerable.Empty<MetlinkService>() : res.Departures;
      }
      catch
      {
        throw;
      }
    }

    public async Task<List<MetlinkCancellationDTO>> GetCancelledBusesFromMetlink()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/trip-cancellations");
        List<MetlinkCancellationDTO> res = null;

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<List<MetlinkCancellationDTO>>(responseStream);
        }
        else
        {
          Console.WriteLine("Error in GetBusesFromStopId");
        }

        return res;
      }
      catch
      {
        throw;
      }
    }

    public async Task<List<TripUpdateHolder>> GetTripUpdates()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs-rt/tripupdates");
        BusTripDTO res = new BusTripDTO();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<BusTripDTO>(responseStream);
        }
        else
        {
          Console.WriteLine("Error in GetTripUpdates");
        }

        return res.Trips;
      }
      catch
      {
        throw;
      }
    }

    public async Task<List<VehiclePositionHolder>> GetVehiclePositions()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs-rt/vehiclepositions");
        VehiclePostionDTO res = new VehiclePostionDTO();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<VehiclePostionDTO>(responseStream);
        }
        else
        {
          Console.WriteLine("Error in GetVehiclePositions");
        }

        return res.VehiclePositions;
      }
      catch
      {
        throw;
      }
    }

    public async Task<List<MetlinkTripDTO>> GetTrips()
    {
      try
      {
        DateTime utcTime = DateTime.UtcNow;
        TimeZoneInfo serverZone = TimeZoneInfo.FindSystemTimeZoneById("NZ");
        DateTime currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, serverZone);

        string startDate = currentDateTime.ToString("yyyy-MM-dd") + "T00%3A00%3A00";
        string endDate = currentDateTime.ToString("yyyy-MM-dd") + "T23%3A59%3A59";
        string query = "?start=" + startDate + "&end=" + endDate;

        var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs/trips" + query);
        List<MetlinkTripDTO> res = new List<MetlinkTripDTO>();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<List<MetlinkTripDTO>>(responseStream);
        }
        else
        {
          Console.WriteLine("Error in GetTrips");
        }

        return res;
      }
      catch
      {
        throw;
      }
    }

    public async Task<List<RouteDTO>> GetRoutes()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs/routes");
        List<RouteDTO> res = new List<RouteDTO>();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<List<RouteDTO>>(responseStream);
        }
        else
        {
          Console.WriteLine("Error in GetTrips");
        }

        return res;
      }
      catch
      {
        throw;
      }
    }

    private async Task<HttpResponseMessage> MakeAPIRequest(string url)
    {
      var attempts = 5;
      while (attempts > 0)
      {
        var request = new HttpRequestMessage(
          HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("x-api-key", _configuration.GetConnectionString("MetlinkAPIKey"));
        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
          Console.WriteLine("Success calling " + url);
          return response;
        }
        Console.WriteLine("Failed calling " + url);
        attempts--;
      }

      throw new Exception("Couldn't get a 200 from Metlink's API");
    }

  }
}

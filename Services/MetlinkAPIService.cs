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

    public async Task<List<MetlinkService>> GetServicesUpdates()
    {

      try
      {
        var tripUpdatesTask = GetTripUpdates();
        var tripsTask = GetTrips();
        var routesTask = GetRoutes();
        var positionsTask = GetVehiclePositions();
        var cancelledServicesTask = GetCancelledServicesFromMetlink();

        await Task.WhenAll(tripUpdatesTask, tripsTask, routesTask, positionsTask, cancelledServicesTask);

        var tripUpdates = await tripUpdatesTask;
        var trips = await tripsTask;
        var routes = await routesTask;
        var positions = await positionsTask;
        var cancelledServices = await cancelledServicesTask;

        var allServices = new List<MetlinkService>();

        if (tripUpdates.Count > 0)
        {
          _logger.LogInformation("Parsing services from trip updates...");
          allServices = ParseServicesFromTripUpdates(tripUpdates);
          _logger.LogInformation("Finished parsing service trips");
        }

        allServices.ForEach(service =>
        {
          var tripThatServiceIsOn = trips.Find(trip => trip.TripId == service.TripId);
          var positionForService = positions.Find(pos => pos.VehiclePosition.Vehicle.Id == service.VehicleId);
          var routeThatServiceIsOn = routes.Find(route => route.RouteId == tripThatServiceIsOn.RouteId);

          if (routeThatServiceIsOn != null)
          {
            service.RouteId = routeThatServiceIsOn.RouteId;
            service.RouteDescription = routeThatServiceIsOn.RouteDesc;
            service.RouteShortName = routeThatServiceIsOn.RouteShortName;
            service.RouteLongName = routeThatServiceIsOn.RouteLongName;
            if (service.TripId.Contains("RAIL") || service.TripId.Contains("rail"))
            {
              Console.WriteLine(service.TripId);
            }

          }
          else
          {
            _logger.LogError($"Route that the service {service.VehicleId} is on is null");
          }
          if (positionForService != null)
          {
            service.Bearing = positionForService.VehiclePosition.Position.Bearing;
            service.Lat = positionForService.VehiclePosition.Position.Latitude;
            service.Long = positionForService.VehiclePosition.Position.Longitude;
          }
          else
          {
            _logger.LogError($"Position for service {service.VehicleId} is null");
          }
        });

        cancelledServices.ForEach(cancellation =>
        {

          var route = routes.Find(route => route.RouteId == cancellation.RouteId);

          if (route == null)
          {
            return;
          }

          allServices.Add(new MetlinkService()
          {
            Status = "CANCELLED",
            RouteLongName = route.RouteLongName,
            RouteShortName = route.RouteLongName,
            VehicleId = System.Guid.NewGuid().ToString()
          });
        });

        return allServices;
      }
      catch
      {
        throw;
      }
    }

    private List<MetlinkService> ParseServicesFromTripUpdates(List<TripUpdateHolder> trips)
    {

      List<MetlinkService> allServices = new List<MetlinkService>();

      trips.ToList().ForEach(trip =>
      {
        var service = new MetlinkService();

        service.VehicleId = trip.TripUpdate.Vehicle.Id;
        int delay = trip.TripUpdate.StopTimeUpdate.Arrival.Delay;
        if (delay > 120)
        {
          service.Status = "LATE";
        }
        else if (delay < -90)
        {
          service.Status = "EARLY";
        }
        else if (delay == 0)
        {
          service.Status = "UNKNOWN";
        }
        else
        {
          service.Status = "ONTIME";
        }
        service.TripId = trip.TripUpdate.Trip.TripId;
        service.StopId = trip.TripUpdate.StopTimeUpdate.StopId;
        service.Delay = trip.TripUpdate.StopTimeUpdate.Arrival.Delay;
        if (allServices.Find(toFind => service.VehicleId == toFind.VehicleId) == null)
        {
          allServices.Add(service);
        }
      });

      return allServices;
    }

    public async Task<IEnumerable<MetlinkService>> GetServicesFromStopId(string stopId)
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
          Console.WriteLine("Error in GetServicesFromStopId");
        }

        return res == null ? Enumerable.Empty<MetlinkService>() : res.Departures;
      }
      catch
      {
        throw;
      }
    }

    public async Task<List<MetlinkCancellationDTO>> GetCancelledServicesFromMetlink()
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
          Console.WriteLine("Error in GetServicesFromStopId");
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
        MetlinkTripUpdatesResponse res = new MetlinkTripUpdatesResponse();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<MetlinkTripUpdatesResponse>(responseStream);
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

    public async Task<List<MetlinkTripResponse>> GetTrips()
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
        List<MetlinkTripResponse> res = new List<MetlinkTripResponse>();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<List<MetlinkTripResponse>>(responseStream);
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

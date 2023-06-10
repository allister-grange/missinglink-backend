using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using missinglink.Models;
using missinglink.Models.VehiclePositions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using missinglink.Metlink.Repository;

namespace missinglink.Services
{
  public class MetlinkAPIService
  {

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetlinkAPIService> _logger;
    private readonly IMetlinkServiceRepository _metlinkServiceRepository;

    public MetlinkAPIService(ILogger<MetlinkAPIService> logger, IHttpClientFactory clientFactory, IConfiguration configuration, IMetlinkServiceRepository metlinkServiceRepository)
    {
      _httpClient = clientFactory.CreateClient("metlinkService");
      _configuration = configuration;
      _logger = logger;
      _metlinkServiceRepository = metlinkServiceRepository;
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

    public async Task<List<MetlinkCancellationResponse>> GetCancelledServicesFromMetlink()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/trip-cancellations");
        List<MetlinkCancellationResponse> res = null;

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<List<MetlinkCancellationResponse>>(responseStream);
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
        VehiclePositionResponse res = new VehiclePositionResponse();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<VehiclePositionResponse>(responseStream);
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

    public async Task<List<MetlinkRouteResponse>> GetRoutes()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs/routes");
        List<MetlinkRouteResponse> res = new List<MetlinkRouteResponse>();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<List<MetlinkRouteResponse>>(responseStream);
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

    public IEnumerable<MetlinkService> GetServices()
    {
      try
      {
        return _metlinkServiceRepository.GetAll();
      }
      catch
      {
        throw;
      }
    }

    public IEnumerable<ServiceStatistic> GetServiceStatisticsByDate(DateTime startDate, DateTime endDate)
    {
      return _metlinkServiceRepository.GetServiceStatisticsByDate(startDate, endDate);
    }

    public Task AddStatisticAsync(ServiceStatistic statistic)
    {
      return _metlinkServiceRepository.AddStatisticAsync(statistic);
    }

    public Task AddServicesAsync(List<MetlinkService> services)
    {
      return _metlinkServiceRepository.AddServicesAsync(services);
    }

    public void DeleteAllServices()
    {
      _metlinkServiceRepository.DeleteAllServices();
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using missinglink.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using missinglink.Repository;
using missinglink.Models.AT;
using System.Text.Json;
using Newtonsoft.Json;
using missinglink.Models.AT.ServiceAlert;
using System.IO;

namespace missinglink.Services
{
  public class AtAPIService
  {
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AtAPIService> _logger;
    private readonly IServiceRepository _serviceRepository;
    private readonly JsonSerializerOptions options = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };

    public AtAPIService(ILogger<AtAPIService> logger, IHttpClientFactory clientFactory, IConfiguration configuration, IServiceRepository serviceRepository)
    {
      _httpClient = clientFactory.CreateClient("AService");
      _configuration = configuration;
      _logger = logger;
      _serviceRepository = serviceRepository;
    }

    public async Task<List<Service>> GetLatestServiceDataFromAT()
    {
      try
      {
        // Get all the trips 
        var tripUpdatesTask = GetTripUpdates();
        var positionsTask = GetVehiclePositions();
        var routesTask = GetRoutes();
        var cancelledTask = GetCancelledAlerts();

        await Task.WhenAll(tripUpdatesTask, cancelledTask, positionsTask, routesTask);

        var tripUpdates = await tripUpdatesTask;
        var positions = await positionsTask;
        var routes = await routesTask;
        var cancellations = await cancelledTask;

        tripUpdates.RemoveAll(trip => trip.TripUpdate == null);

        var allServices = ParseATResponsesIntoServices(tripUpdates, positions, routes);

        var cancelledServicesToBeAdded = GetCancelledServicesToBeAdded(cancellations, routes, tripUpdates);

        allServices.AddRange(cancelledServicesToBeAdded);

        return allServices;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An error occurred while retrieving service updates for AT.");
        throw;
      }
    }

    private List<Service> ParseATResponsesIntoServices(List<Entity> tripUpdates,
      List<PositionResponseEntity> positions, List<Datum> routes)
    {
      TimeZoneInfo nzTimeZone = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
      DateTime nzDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, nzTimeZone);
      string formattedDate = nzDateTime.ToString("yyyyMMdd");
      string formattedTime = nzDateTime.ToString("HH:mm:ss");

      var newServices = new List<Service>();

      var tripUpdatesWithUniqueVehicleIds = tripUpdates.GroupBy(e => e.TripUpdate?.Vehicle?.Id)
            .Select(g => g.OrderByDescending(e => e.TripUpdate.Delay).First())
            .ToList();

      var tripUpdatesOnlyToday = tripUpdatesWithUniqueVehicleIds
          .Where(trip =>
          {
            return trip.TripUpdate.Trip.StartDate == formattedDate && trip.TripUpdate.Trip.StartTime.CompareTo(formattedTime) < 0;
          })
          .ToList();

      string json = JsonConvert.SerializeObject(tripUpdatesOnlyToday, Formatting.Indented);
      string filePath = "output.json";
      File.WriteAllText(filePath, json);

      foreach (var trip in tripUpdatesOnlyToday)
      {
        var newService = new Service();

        // find route the trip is on 
        var routeForTrip = routes.FirstOrDefault(route => route.Id == trip.TripUpdate.Trip?.RouteId);

        if (routeForTrip != null)
        {
          newService.RouteId = routeForTrip.Id;
          newService.RouteDescription = routeForTrip.Attributes.RouteLongName;
          newService.RouteShortName = routeForTrip.Attributes.RouteShortName;
          newService.RouteLongName = routeForTrip.Attributes.RouteLongName;
          newService.ServiceName = routeForTrip.Attributes.RouteShortName;
        }

        // find the position of the service in the trip
        var positionForTrip = positions.FirstOrDefault(position => position.Id == trip.TripUpdate?.Vehicle?.Id);

        if (positionForTrip != null)
        {
          newService.Lat = positionForTrip.Vehicle.Position.Latitude;
          newService.Long = positionForTrip.Vehicle.Position.Longitude;
          // todo this might error out
          newService.Bearing = positionForTrip.Vehicle.Position.Bearing;
        }

        newService.Delay = trip.TripUpdate.Delay;
        if (newService.Delay > 120)
        {
          newService.Status = "LATE";
        }
        else if (newService.Delay < -90)
        {
          newService.Status = "EARLY";
        }
        else if (newService.Delay == 0)
        {
          newService.Status = "UNKNOWN";
        }
        else
        {
          newService.Status = "ONTIME";
        }

        newService.ProviderId = "AT";
        newService.TripId = trip.Id;
        newService.VehicleId = trip.TripUpdate.Vehicle?.Id;
        newService.VehicleType = GetVehicleType(routeForTrip.Attributes.RouteType);

        newServices.Add(newService);
      }

      return newServices;
    }
    public async Task UpdateServicesWithLatestData(List<Service> allServices)
    {
      try
      {
        await _serviceRepository.AddServicesAsync(allServices);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An error occurred while updates services in the db");
        throw ex;
      }
    }


    private List<Service> GetCancelledServicesToBeAdded(List<ServiceAlertEntity> cancelledServices, List<Datum> routes, List<Entity> tripUpdates)
    {
      var cancelledServicesToBeAdded = new List<Service>();

      foreach (var cancellation in cancelledServices)
      {

        if (cancellation.Alert.Effect != "NO_SERVICE")
        {
          continue;
        }

        var tripUpdateId = cancellation.Alert.InformedEntity.FirstOrDefault(informedEntity => informedEntity.Trip?.TripId != null);

        if (tripUpdateId == null)
        {
          continue;
        }

        // make sure the active period matches the current time.....

        var trip = tripUpdates.Find(trip => trip.TripUpdate.Trip.TripId == tripUpdateId.Trip.TripId);

        if (trip == null)
        {
          Console.WriteLine("Found a cancellation with a tripID that isn't in the tripupdates for AT");
          continue;
        }

        var route = routes.Find(route => route.Id == trip.TripUpdate.Trip.RouteId);

        if (route == null)
        {
          Console.WriteLine("Found a route with an that isn't in the tripupdates for AT");
          continue;
        }

        var cancelledService = new Service()
        {
          Status = "CANCELLED",
          TripId = tripUpdateId.Trip.TripId,
          RouteId = route.Id,
          RouteDescription = route.Attributes.RouteLongName,
          RouteShortName = route.Attributes.RouteShortName,
          RouteLongName = route.Attributes.RouteLongName,
          ProviderId = "AT",
          VehicleType = GetVehicleType(route.Attributes.RouteType)
        };

        cancelledServicesToBeAdded.Add(cancelledService);
      }

      return cancelledServicesToBeAdded;
    }

    private string GetVehicleType(int routeType)
    {
      switch (routeType)
      {
        case 3:
          return "Bus";
        case 2:
          return "Train";
        case 4:
          return "Ferry";
        case 712:
          return "Bus";
        default:
          return "Bus";
      }
    }

    public async Task<List<Entity>> GetTripUpdates()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.at.govt.nz/realtime/legacy/tripupdates");
        AtTripUpdatesResponse res = new AtTripUpdatesResponse();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<AtTripUpdatesResponse>(responseStream);
        }
        else
        {
          _logger.LogError("Error making API call to: GetTripUpdates");
        }

        return res.Response.Entity;
      }
      catch
      {
        throw;
      }
    }

    public async Task<List<ServiceAlertEntity>> GetCancelledAlerts()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.at.govt.nz/realtime/legacy/servicealerts");
        var res = new AtServiceAlert();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<AtServiceAlert>(responseStream);
        }
        else
        {
          _logger.LogError("Error making API call to: GetCancelledAlerts");
        }

        return res.Response.Entity;
      }
      catch
      {
        throw;
      }
    }

    public async Task<List<PositionResponseEntity>> GetVehiclePositions()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.at.govt.nz/realtime/legacy/vehiclelocations");
        AtVehiclePositionResponse res = new AtVehiclePositionResponse();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<AtVehiclePositionResponse>(responseStream);
        }
        else
        {
          _logger.LogError("Error making API call to: GetVehiclePositions");
        }

        return res.Response.Entity;
      }
      catch
      {
        throw;
      }
    }

    // public async Task<List<ATripResponse>> GetTrips()
    // {
    //   try
    //   {
    //     DateTime utcTime = DateTime.UtcNow;
    //     TimeZoneInfo serverZone = TimeZoneInfo.FindSystemTimeZoneById("NZ");
    //     DateTime currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, serverZone);

    //     string startDate = currentDateTime.ToString("yyyy-MM-dd") + "T00%3A00%3A00";
    //     string endDate = currentDateTime.ToString("yyyy-MM-dd") + "T23%3A59%3A59";
    //     string query = "?start=" + startDate + "&end=" + endDate;

    //     var response = await MakeAPIRequest("https://api.opendata.A.org.nz/v1/gtfs/trips" + query);
    //     List<ATripResponse> res = new List<ATripResponse>();

    //     if (response.IsSuccessStatusCode)
    //     {
    //       var responseStream = await response.Content.ReadAsStringAsync();
    //       res = JsonConvert.DeserializeObject<List<ATripResponse>>(responseStream);
    //     }
    //     else
    //     {
    //       _logger.LogError("Error making API call to: GetTrips");
    //     }

    //     return res;
    //   }
    //   catch
    //   {
    //     throw;
    //   }
    // }

    public async Task<List<Datum>> GetRoutes()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.at.govt.nz/gtfs/v3/routes");
        var res = new AtRouteResponse();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<AtRouteResponse>(responseStream);
        }
        else
        {
          _logger.LogError("Error making API call to: GetRoutes");
        }

        return res.Data;
      }
      catch
      {
        throw;
      }
    }

    // todo the filtering here for only AT
    public async Task<IEnumerable<Service>> GetLatestServices()
    {
      try
      {
        var batchId = await _serviceRepository.GetLatestBatchId();
        return _serviceRepository.GetByBatchId(batchId);
      }
      catch
      {
        throw;
      }
    }

    // todo the filtering here for only AT

    public IEnumerable<ServiceStatistic> GetServiceStatisticsByDate(DateTime startDate, DateTime endDate)
    {
      return _serviceRepository.GetServiceStatisticsByDate(startDate, endDate);
    }

    private async Task<HttpResponseMessage> MakeAPIRequest(string url)
    {
      var attempts = 5;
      while (attempts > 0)
      {
        var request = new HttpRequestMessage(
          HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _configuration.GetConnectionString("AtAPIKey"));
        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
          Console.WriteLine("Success calling " + url);
          return response;
        }
        Console.WriteLine("Failed calling " + url);
        attempts--;
      }

      throw new Exception("Couldn't get a 200 from A's API");
    }

  }
}

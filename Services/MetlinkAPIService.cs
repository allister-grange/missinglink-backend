using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using missinglink.Models.Metlink;
using missinglink.Models.Metlink.VehiclePosition;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using missinglink.Repository;
using missinglink.Models;

namespace missinglink.Services
{
  public class MetlinkAPIService
  {
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetlinkAPIService> _logger;
    private readonly IServiceRepository _metlinkServiceRepository;

    public MetlinkAPIService(ILogger<MetlinkAPIService> logger, IHttpClientFactory clientFactory, IConfiguration configuration, IServiceRepository metlinkServiceRepository)
    {
      _httpClient = clientFactory.CreateClient("metlinkService");
      _configuration = configuration;
      _logger = logger;
      _metlinkServiceRepository = metlinkServiceRepository;
    }

    public async Task<List<Service>> GetLatestServiceDataFromMetlink()
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

        var allServices = await ParseServicesFromTripUpdates(tripUpdates);

        // todo this is bad practise (not cloning the array)
        UpdateServicesWithRoutesAndPositions(allServices, trips, routes, positions);

        var cancelledServicesToBeAdded = GetCancelledServicesToBeAdded(cancelledServices, routes);

        allServices.AddRange(cancelledServicesToBeAdded);

        allServices.ForEach((service) =>
        {
          service.ProviderId = "Metlink";
          service.ServiceName = service.RouteShortName;
          if (int.TryParse(service.RouteShortName, out _))
          {
            service.VehicleType = "BUS";
          }
          else
          {
            service.VehicleType = "TRAIN";
          }
        });

        return allServices;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An error occurred while retrieving service updates.");
        throw;
      }

    }

    private List<Service> GetCancelledServicesToBeAdded(List<MetlinkCancellationResponse> cancelledServices, List<RouteResponse> routes)
    {
      var cancelledServicesToBeAdded = new List<Service>();

      foreach (var cancellation in cancelledServices)
      {
        var route = routes.Find(route => route.RouteId == cancellation.RouteId);

        if (route != null)
        {
          var cancelledService = new Service()
          {
            Status = "CANCELLED",
            TripId = cancellation.TripId,
            RouteId = route.RouteId,
            RouteDescription = route.RouteDesc,
            RouteShortName = route.RouteShortName,
            RouteLongName = route.RouteLongName,
          };

          cancelledServicesToBeAdded.Add(cancelledService);
        }
      }

      return cancelledServicesToBeAdded;
    }


    private async Task<List<Service>> ParseServicesFromTripUpdates(List<TripUpdateHolder> tripUpdates)
    {
      List<Service> allServices = new List<Service>();

      tripUpdates.ToList().ForEach(trip =>
      {
        var service = new Service();

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
        service.Delay = trip.TripUpdate.StopTimeUpdate.Arrival.Delay;
        if (allServices.Find(toFind => service.VehicleId == toFind.VehicleId) == null)
        {
          allServices.Add(service);
        }
      });

      return allServices;
    }

    private void UpdateServicesWithRoutesAndPositions(List<Service> services, List<MetlinkTripResponse> trips, List<RouteResponse> routes, List<VehiclePositionHolder> positions)
    {
      foreach (var service in services)
      {
        var tripThatServiceIsOn = trips.Find(trip => trip.TripId == service.TripId);
        var positionForService = positions.Find(pos => pos.VehiclePosition.Vehicle.Id == service.VehicleId);
        var routeThatServiceIsOn = routes.Find(route => route.RouteId == tripThatServiceIsOn?.RouteId);

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
      }
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
          _logger.LogError("Error making API call to: GetServicesFromStopId");
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
          _logger.LogError("Error making API call to: GetTripUpdates");
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
          _logger.LogError("Error making API call to: GetVehiclePositions");
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
          _logger.LogError("Error making API call to: GetTrips");
        }

        return res;
      }
      catch
      {
        throw;
      }
    }

    public async Task<List<RouteResponse>> GetRoutes()
    {
      try
      {
        var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs/routes");
        List<RouteResponse> res = new List<RouteResponse>();

        if (response.IsSuccessStatusCode)
        {
          var responseStream = await response.Content.ReadAsStringAsync();
          res = JsonConvert.DeserializeObject<List<RouteResponse>>(responseStream);
        }
        else
        {
          _logger.LogError("Error making API call to: GetRoutes");
        }

        return res;
      }
      catch
      {
        throw;
      }
    }

    public async Task<IEnumerable<Service>> GetLatestServices()
    {
      try
      {
        var batchId = await _metlinkServiceRepository.GetLatestBatchId();
        return _metlinkServiceRepository.GetByBatchId(batchId);
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

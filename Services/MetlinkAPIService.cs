using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using missinglink.Models;
using missinglink.Models.VehiclePositions;
using Microsoft.Extensions.Configuration;

namespace missinglink.Services
{
  public class MetlinkAPIServices
  {

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;


    public MetlinkAPIServices(IHttpClientFactory clientFactory, IConfiguration configuration)
    {
      _httpClient = clientFactory.CreateClient("metlinkService");
      _configuration = configuration;
    }
    public async Task<IEnumerable<Bus>> GetBusesFromStopId(string stopId)
    {
      var response = await MakeAPIRequest($"https://api.opendata.metlink.org.nz/v1/stop-predictions?stop_id={stopId}");
      BusRoute res = null;

      if (response.IsSuccessStatusCode)
      {
        using var responseStream = await response.Content.ReadAsStreamAsync();
        res = await JsonSerializer.DeserializeAsync<BusRoute>(responseStream);
      }
      else
      {
        Console.WriteLine("Error in GetBusesFromStopId");
      }

      return res == null ? Enumerable.Empty<Bus>() : res.Departures;
    }

    public async Task<ServiceAlertDto> GetCancelledBusesFromMetlink()
    {
      var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs-rt/servicealerts");
      ServiceAlertDto res = null;

      if (response.IsSuccessStatusCode)
      {
        using var responseStream = await response.Content.ReadAsStreamAsync();
        res = await JsonSerializer.DeserializeAsync<ServiceAlertDto>(responseStream);
      }
      else
      {
        Console.WriteLine("Error in GetBusesFromStopId");
      }

      return res;
    }

    public async Task<List<TripUpdateHolder>> GetTripUpdates()
    {
      var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs-rt/tripupdates");
      BusTripDTO res = new BusTripDTO();

      if (response.IsSuccessStatusCode)
      {
        using var responseStream = await response.Content.ReadAsStreamAsync();
        res = await JsonSerializer.DeserializeAsync<BusTripDTO>(responseStream);
      }
      else
      {
        Console.WriteLine("Error in GetTripUpdates");
      }

      return res.Trips;
    }

    public async Task<List<VehiclePositionHolder>> GetVehiclePositions()
    {
      var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs-rt/vehiclepositions");
      VehiclePostionDTO res = new VehiclePostionDTO();

      if (response.IsSuccessStatusCode)
      {
        using var responseStream = await response.Content.ReadAsStreamAsync();
        res = await JsonSerializer.DeserializeAsync<VehiclePostionDTO>(responseStream);
      }
      else
      {
        Console.WriteLine("Error in GetVehiclePositions");
      }

      return res.VehiclePositions;
    }

    public async Task<List<TripDTO>> GetTrips()
    {
      DateTime utcTime = DateTime.UtcNow;
      TimeZoneInfo serverZone = TimeZoneInfo.FindSystemTimeZoneById("NZ");
      DateTime currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, serverZone);

      string startDate = currentDateTime.ToString("yyyy-MM-dd") + "T00%3A00%3A00";
      string endDate = currentDateTime.ToString("yyyy-MM-dd") + "T23%3A59%3A59";
      string query = "?start=" + startDate + "&end=" + endDate;

      var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs/trips" + query);
      List<TripDTO> res = new List<TripDTO>();

      if (response.IsSuccessStatusCode)
      {
        using var responseStream = await response.Content.ReadAsStreamAsync();
        res = await JsonSerializer.DeserializeAsync<List<TripDTO>>(responseStream);
      }
      else
      {
        Console.WriteLine("Error in GetTrips");
      }

      return res;
    }

    public async Task<List<RouteDTO>> GetRoutes()
    {
      var response = await MakeAPIRequest("https://api.opendata.metlink.org.nz/v1/gtfs/routes");
      List<RouteDTO> res = new List<RouteDTO>();

      if (response.IsSuccessStatusCode)
      {
        using var responseStream = await response.Content.ReadAsStreamAsync();
        res = await JsonSerializer.DeserializeAsync<List<RouteDTO>>(responseStream);
      }
      else
      {
        Console.WriteLine("Error in GetTrips");
      }

      return res;
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

        if(response.IsSuccessStatusCode) {
          Console.WriteLine("Success calling " + url);
          return response;
        }
        Console.WriteLine("Failed calling " + url);
        attempts --;
      }

      throw new Exception("Couldn't get a 200 from Metlink's API");
    }

  }
}

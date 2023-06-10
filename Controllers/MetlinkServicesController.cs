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
  public class MetlinkServicesController : ControllerBase
  {
    private readonly ILogger<MetlinkServicesController> _logger;
    private readonly MetlinkAPIService _MetlinkAPIService;
    public MetlinkServicesController(ILogger<MetlinkServicesController> logger, ServiceContext ServiceContext,
      MetlinkAPIService MetlinkAPIService)
    {
      _logger = logger;
      _MetlinkAPIService = MetlinkAPIService;
    }

    [HttpGet("updates")]
    public async Task<IEnumerable<MetlinkService>> GetServiceTripUpdates()
    {
      var services = await _MetlinkAPIService.GetLatestServices();

      if (services == null || services.Count() == 0)
      {
        throw new Exception("Services table in database not populated.");
      }

      return services;
    }

    [HttpGet("statistics")]
    public IActionResult GetServiceStatistics(string startDate, string endDate)
    {

      _logger.LogInformation("passed in dates: " + startDate + " " + endDate);
      IEnumerable<ServiceStatistic> stats = null;

      if (String.IsNullOrEmpty(startDate) || String.IsNullOrEmpty(endDate))
      {
        return BadRequest();
      }

      DateTime startDateInput;
      DateTime endDateInput;

      try
      {
        startDateInput = DateTime.Parse(startDate);
        endDateInput = DateTime.Parse(endDate);

        stats = _MetlinkAPIService.GetServiceStatisticsByDate(startDateInput, endDateInput);
      }
      catch (System.FormatException e)
      {
        _logger.LogError($"Your date inputs were formatted incorrectly {e.ToString()}");
        throw new ArgumentException("Your date inputs were formatted incorrectly");
      }
      _logger.LogInformation("Parsed dates: " + startDateInput + " " + endDateInput);
      if (stats == null || stats.Count() == 0)
      {
        throw new Exception("ServiceStatistic table in database not populated. Try calling UpdateServiceStatistics.");
      }

      return Ok(stats);
    }

    [HttpPost("statistics")]
    public async Task<ActionResult> UpdateServiceStatistics()
    {
      var newBatchId = await _MetlinkAPIService.GenerateNewBatchId();
      var allServices = await _MetlinkAPIService.GetServicesUpdates(newBatchId);
      if (allServices.Count > 0)
      {
        await _MetlinkAPIService.AddServicesAsync(allServices);
      }

      var newServiceStatistic = new ServiceStatistic();

      if (allServices == null)
      {
        return NotFound("The service table must be empty");
      }

      newServiceStatistic.DelayedServices = allServices.Where(service => service.Status == "LATE").Count();
      newServiceStatistic.EarlyServices = allServices.Where(service => service.Status == "EARLY").Count();
      newServiceStatistic.NotReportingTimeServices = allServices.Where(service => service.Status == "UNKNOWN").Count();
      newServiceStatistic.OnTimeServices = allServices.Where(service => service.Status == "ONTIME").Count();
      newServiceStatistic.CancelledServices = allServices.Where(service => service.Status == "CANCELLED").Count();
      newServiceStatistic.TotalServices = allServices.Where(service => service.Status != "CANCELLED").Count();
      DateTime utcTime = DateTime.UtcNow;
      TimeZoneInfo serverZone = TimeZoneInfo.FindSystemTimeZoneById("NZ");
      DateTime currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, serverZone);
      newServiceStatistic.Timestamp = currentDateTime;
      newServiceStatistic.BatchId = newBatchId;

      await _MetlinkAPIService.AddStatisticAsync(newServiceStatistic);
      return Ok();
    }
  }
}

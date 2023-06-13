using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using missinglink.Models;
using missinglink.Services;

namespace missinglink.Controllers
{
  [ApiController]
  [Route("api/v1/at")]
  public class AtServicesController : ControllerBase
  {
    private readonly ILogger<AtServicesController> _logger;
    private readonly MetlinkAPIService _metlinkAPIService;
    public AtServicesController(ILogger<AtServicesController> logger,
      MetlinkAPIService metlinkAPIService)
    {
      _logger = logger;
      _metlinkAPIService = metlinkAPIService;
    }

    [HttpGet("services")]
    public async Task<IEnumerable<Service>> GetNewestServices()
    {
      _logger.LogInformation("Fetching services request");
      var services = await _metlinkAPIService.GetLatestServices();

      if (services == null || services.Count() == 0)
      {
        throw new Exception("Services table in database not populated.");
      }

      _logger.LogInformation("Found " + services.Count() + " services");
      return services;
    }

    [HttpGet("statistics")]
    public IActionResult GetServiceStatisticsByDate(string startDate, string endDate)
    {

      _logger.LogInformation("Fetching statistics with startDate of: " + startDate + " and endDate of:" + endDate);
      IEnumerable<ServiceStatistic> stats = null;

      if (String.IsNullOrEmpty(startDate) || String.IsNullOrEmpty(endDate))
      {
        return BadRequest("You must provide a startState and endData query string");
      }

      DateTime startDateInput;
      DateTime endDateInput;

      try
      {
        startDateInput = DateTime.Parse(startDate);
        endDateInput = DateTime.Parse(endDate);

        stats = _metlinkAPIService.GetServiceStatisticsByDate(startDateInput, endDateInput);
      }
      catch (System.FormatException e)
      {
        _logger.LogError($"Your date inputs were formatted incorrectly {e.ToString()}");
        return BadRequest("Your date inputs were formatted incorrectly");
      }
      _logger.LogInformation("Parsed dates: " + startDateInput + " " + endDateInput);
      if (stats == null || stats.Count() == 0)
      {
        throw new Exception("ServiceStatistic table in database not populated.");
      }

      return Ok(stats);
    }

    [HttpPost("update")]
    public async Task<ActionResult> UpdateServices()
    {
      try
      {
        // generate a new batch ID for these services
        var newBatchId = await _metlinkAPIService.GenerateNewBatchId();

        // update the services table
        var allServices = await _metlinkAPIService.GetLatestServiceDataFromMetlink();

        // update the services with the new batchId
        allServices.ForEach((service) => service.BatchId = newBatchId);

        await _metlinkAPIService.UpdateServicesWithLatestData(allServices);

        // update the statistics table with the new services
        var allStatistics = await _metlinkAPIService.UpdateStatisticsWithLatestServices(allServices, newBatchId);
      }
      catch (System.Exception e)
      {
        throw new Exception("Failed to update services: " + e);
      }

      return Ok();
    }
  }
}

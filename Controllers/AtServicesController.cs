using System;
using System.Collections.Generic;
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
    private readonly AtAPIService _atAPIService;
    public AtServicesController(ILogger<AtServicesController> logger,
      AtAPIService atAPIService)
    {
      _logger = logger;
      _atAPIService = atAPIService;
    }

    [HttpGet("services")]
    public async Task<List<Service>> GetNewestServices()
    {
      _logger.LogInformation("Fetching services request");
      var services = await _atAPIService.GetLatestServices();

      if (services == null || services.Count == 0)
      {
        throw new Exception("Services table in database not populated.");
      }

      _logger.LogInformation("Found " + services.Count + " services");
      return services;
    }

    [HttpGet("statistics")]
    public IActionResult GetServiceStatisticsByDate(string startDate, string endDate)
    {

      _logger.LogInformation("Fetching statistics with startDate of: " + startDate + " and endDate of:" + endDate);
      List<ServiceStatistic> stats = null;

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

        stats = _atAPIService.GetServiceStatisticsByDate(startDateInput, endDateInput);
      }
      catch (System.FormatException e)
      {
        _logger.LogError($"Your date inputs were formatted incorrectly {e.ToString()}");
        return BadRequest("Your date inputs were formatted incorrectly");
      }
      _logger.LogInformation("Parsed dates: " + startDateInput + " " + endDateInput);
      if (stats == null || stats.Count == 0)
      {
        throw new Exception("ServiceStatistic table in database not populated.");
      }

      return Ok(stats);
    }
  }
}

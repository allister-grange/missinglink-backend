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
  [Route("api/v1/updates")]
  public class UpdateServicesController : ControllerBase
  {
    private readonly ILogger<AtServicesController> _logger;
    private readonly AtAPIService _atAPIService;
    private readonly MetlinkAPIService _metlinkAPIService;
    private readonly ServiceAPI _serviceAPI;

    public UpdateServicesController(ILogger<AtServicesController> logger,
      AtAPIService atAPIService, MetlinkAPIService metlinkAPIService, ServiceAPI serviceAPI)
    {
      _logger = logger;
      _atAPIService = atAPIService;
      _metlinkAPIService = metlinkAPIService;
      _serviceAPI = serviceAPI;
    }

    [HttpPost("")]
    public async Task<ActionResult> UpdateServices()
    {
      try
      {
        // generate a new batch ID for these services
        var newBatchId = await _serviceAPI.GenerateNewBatchId();

        // Auckland Transport services
        var atServices = await _atAPIService.GetLatestServiceDataFromAT();

        // Metlink Services
        var metlinkServices = await _metlinkAPIService.GetLatestServiceDataFromMetlink();

        var allServices = new List<Service>();
        allServices.AddRange(atServices);
        allServices.AddRange(metlinkServices);

        // update the services with the new batchId
        allServices.ForEach((service) => service.BatchId = newBatchId);

        await _serviceAPI.UpdateServicesWithLatestData(allServices);

        // // update the statistics table with the new services
        // var allStatistics = await _serviceAPI.UpdateStatisticsWithLatestServices(allServices, newBatchId);
      }
      catch (System.Exception e)
      {
        throw new Exception("Failed to update services: " + e);
      }

      return Ok();
    }
  }
}

using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using missinglink.Models;
using missinglink.Services;
using missinglink.Repository;
using Microsoft.Extensions.Configuration;
using Moq.Protected;
using System.Net;
using System.Text;

public class AtAPIServiceTests
{
  private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
  private readonly Mock<IConfiguration> _mockConfiguration;
  private readonly Mock<ILogger<AtAPIService>> _mockLogger;
  private readonly Mock<IServiceRepository> _mockServiceRepository;
  private readonly Mock<HttpMessageHandler> _mockHandler;
  private readonly AtAPIService _service;

  public AtAPIServiceTests()
  {
    _mockHttpClientFactory = new Mock<IHttpClientFactory>();
    _mockConfiguration = new Mock<IConfiguration>();
    _mockLogger = new Mock<ILogger<AtAPIService>>();
    _mockServiceRepository = new Mock<IServiceRepository>();
    _mockHandler = CreateMockHandler();

    _service = new AtAPIService(_mockLogger.Object, _mockHttpClientFactory.Object, _mockConfiguration.Object, _mockServiceRepository.Object);
  }

  [Fact]
  public async Task FetchLatestTripDataFromUpstreamService_ReturnsExpectedResults()
  {
    var result = await _service.FetchLatestTripDataFromUpstreamService();

    // Assert
    Assert.NotNull(result);
    Assert.IsType<List<Service>>(result);
    // More assertions here based on expected results
  }

  private Mock<HttpMessageHandler> CreateMockHandler()
  {
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
    var tripUpdatesJson = File.ReadAllText("trip_updates.json");
    var routesJson = File.ReadAllText("routes.json");
    var serviceAlertsJson = File.ReadAllText("service_alerts.json");
    var vehicleLocationJson = File.ReadAllText("vehicle_locations.json");

    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.ToString() == "https://api.at.govt.nz/realtime/legacy/tripupdates"),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(tripUpdatesJson, Encoding.UTF8, "application/json"),
        });

    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.ToString() == "https://api.at.govt.nz/realtime/legacy/servicealerts"),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(serviceAlertsJson, Encoding.UTF8, "application/json"),
        });

    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.ToString() == "https://api.at.govt.nz/realtime/legacy/vehiclelocations"),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(vehicleLocationJson, Encoding.UTF8, "application/json"),
        });

    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.ToString() == "https://api.at.govt.nz/gtfs/v3/routes"),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(routesJson, Encoding.UTF8, "application/json"),
        });

    var client = new HttpClient(mockHttpMessageHandler.Object);
    _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

    return mockHttpMessageHandler;
  }

}

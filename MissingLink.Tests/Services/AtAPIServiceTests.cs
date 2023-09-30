using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using missinglink.Models;
using missinglink.Services;
using missinglink.Repository;
using missinglink.Utils;
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
    var mockDateTimeProvider = new Mock<IDateTimeProvider>();
    mockDateTimeProvider.Setup(dtp => dtp.UtcNow).Returns(new DateTime(2023, 9, 29, 23, 59, 59));
    _mockHandler = CreateMockHandler();

    _service = new AtAPIService(_mockLogger.Object, _mockHttpClientFactory.Object, _mockConfiguration.Object, _mockServiceRepository.Object, mockDateTimeProvider.Object);
  }

  [Fact]
  public async Task FetchLatestTripDataFromUpstreamService_ReturnsExpectedResults()
  {
    var result = await _service.FetchLatestTripDataFromUpstreamService();

    // Assert
    Assert.NotNull(result);
    Assert.IsType<List<Service>>(result);

    // Test total item count
    Assert.Equal(546, result.Count());

    // Test a couple of details from specific vehicles
    Assert.Equal(215, result.First().Bearing);
    Assert.Equal(0, result.First().Delay);
    Assert.Equal(-36.925583333333336, result.First().Lat);
    Assert.Equal(174.786545, result.First().Long);
    Assert.Equal("AT", result.First().ProviderId);
    Assert.Equal("ONE", result.First().RouteDescription);
    Assert.Equal("ONE-201", result.First().RouteId);
    Assert.Equal("ONE", result.First().RouteLongName);
    Assert.Equal("ONE", result.First().RouteShortName);
    Assert.Equal("ONE", result.First().ServiceName);
    Assert.Equal("UNKNOWN", result.First().Status);
    Assert.Equal("51100306140-20230927142708_v106.28", result.First().TripId);
    Assert.Equal("59593", result.First().VehicleId);
    Assert.Equal("Train", result.First().VehicleType);

    // Check the route, the bearing, everything etc
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

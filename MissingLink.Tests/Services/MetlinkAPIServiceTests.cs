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

public class MetlinkAPIServiceTests
{
  private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
  private readonly Mock<IConfiguration> _mockConfiguration;
  private readonly Mock<ILogger<MetlinkAPIService>> _mockLogger;
  private readonly Mock<IServiceRepository> _mockServiceRepository;

  public MetlinkAPIServiceTests()
  {
    _mockHttpClientFactory = new Mock<IHttpClientFactory>();
    _mockConfiguration = new Mock<IConfiguration>();
    _mockLogger = new Mock<ILogger<MetlinkAPIService>>();
    _mockServiceRepository = new Mock<IServiceRepository>();
    CreateMockHandler();
  }

  // Test that the first vehicle in the example JSON looks okay
  [Fact]
  public async Task FetchLatestTripDataFromUpstreamService_ReturnsExpectedResultForFirstEntry()
  {
    // Arrange
    var metlinkApiService = new MetlinkAPIService(_mockLogger.Object, _mockHttpClientFactory.Object, _mockConfiguration.Object, _mockServiceRepository.Object);

    // Act
    var services = await metlinkApiService.FetchLatestTripDataFromUpstreamService();

    // Assert
    Assert.NotNull(services);
    Assert.IsType<List<Service>>(services);

    // Test total services count
    Assert.Equal(190, services.Count());

    // Test the details from the first vehicle in the trip updates
    Service service = services.First();
    Assert.Equal(54, service.Bearing);
    Assert.Equal(96, service.Delay);
    Assert.Equal(-41.1279259, service.Lat);
    Assert.Equal(175.0496826, service.Long);
    Assert.Equal("Metlink", service.ProviderId);
    Assert.Equal("Petone - Lower Hutt - Upper Hutt - Emerald Hill", service.RouteDescription);
    Assert.Equal("1100", service.RouteId);
    Assert.Equal("Emerald Hill - Upper Hutt - Lower Hutt - Petone", service.RouteLongName);
    Assert.Equal("110", service.RouteShortName);
    Assert.Equal("110", service.ServiceName);
    Assert.Equal("ONTIME", service.Status);
    Assert.Equal("110__0__719__TZM__404__404_20230924", service.TripId);
    Assert.Equal("3316", service.VehicleId);
    Assert.Equal("BUS", service.VehicleType);
  }



  private Mock<HttpMessageHandler> CreateMockHandler()
  {
    var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
    var tripUpdatesJson = File.ReadAllText("metlink/trip_updates.json");
    var routesJson = File.ReadAllText("metlink/routes.json");
    var cancellationsJson = File.ReadAllText("metlink/cancellations.json");
    var tripsJson = File.ReadAllText("metlink/trips.json");
    var vehiclePositionsJson = File.ReadAllText("metlink/vehicle_positions.json");

    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.ToString() == "https://api.opendata.metlink.org.nz/v1/gtfs-rt/tripupdates"),
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
            ItExpr.Is<HttpRequestMessage>(request =>
            request.Method == HttpMethod.Get &&
            request.RequestUri!.GetLeftPart(UriPartial.Path) == "https://api.opendata.metlink.org.nz/v1/trip-cancellations"),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(cancellationsJson, Encoding.UTF8, "application/json"),
        });

    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.ToString() == "https://api.opendata.metlink.org.nz/v1/gtfs-rt/vehiclepositions"),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(vehiclePositionsJson, Encoding.UTF8, "application/json"),
        });

    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(request =>
            request.Method == HttpMethod.Get &&
            request.RequestUri!.GetLeftPart(UriPartial.Path) == "https://api.opendata.metlink.org.nz/v1/gtfs/trips"),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(tripsJson, Encoding.UTF8, "application/json"),
        });

    mockHttpMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.ToString() == "https://api.opendata.metlink.org.nz/v1/gtfs/routes"),
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

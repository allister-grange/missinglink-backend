using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using missinglink.Services;

public class MetlinkServicesHub : Hub
{
  private readonly MetlinkAPIService _metlinkAPIService;
  private readonly List<string> connectionIds = new List<string>();

  public MetlinkServicesHub(MetlinkAPIService MetlinkAPIService)
  {
    _metlinkAPIService = MetlinkAPIService;
    StartTimer();
  }

  private async Task StartTimer()
  {
    await SendServicesUpdate();
    var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(200));
    while (await periodicTimer.WaitForNextTickAsync())
    {
      await SendServicesUpdate();
    }
  }
  public async Task SendServicesUpdate()
  {
    if (connectionIds.Count > 0)
    {
      // get service updates
      try
      {
        // var services = await _metlinkAPIService.GetServicesUpdates();

        // ship them to the user
        // await base.Clients.All.SendAsync("ServiceUpdates", services);
      }
      catch
      {
        Console.WriteLine("Failed to send out update, there's an issue with the API");
      }
    }
  }

  public override async Task OnConnectedAsync()
  {
    connectionIds.Add(Context.ConnectionId);
    await base.OnConnectedAsync();
  }

  // We want to create a group so we can count all active connections
  public override async Task OnDisconnectedAsync(Exception exception)
  {
    connectionIds.Remove(Context.ConnectionId);
    await base.OnDisconnectedAsync(exception);
  }

}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using missinglink.Services;

public class BusHub : Hub
{
  private readonly MetlinkAPIServices _MetlinkAPIService;
  private readonly List<string> connectionIds = new List<string>();

  public BusHub(MetlinkAPIServices MetlinkAPIService)
  {
    _MetlinkAPIService = MetlinkAPIService;
    StartTimer();
  }

  private async void StartTimer()
  {
    var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));
    while (await periodicTimer.WaitForNextTickAsync())
    {
      await SendBusesUpdate();
    }
  }
  public async Task SendBusesUpdate()
  {
    if (connectionIds.Count > 0)
    {
      // get bus updates
      var buses = await _MetlinkAPIService.GetBusUpdates();

      // ship them to the user
      Console.WriteLine("Sending out bus updates of " + buses.Count + " buses to: " + connectionIds.Count + " client/s.");
      await base.Clients.All.SendAsync("BusUpdates", buses);
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
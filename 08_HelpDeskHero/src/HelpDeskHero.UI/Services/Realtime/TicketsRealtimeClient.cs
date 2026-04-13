using HelpDeskHero.Shared.Contracts.Tickets;
using HelpDeskHero.UI.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace HelpDeskHero.UI.Services.Realtime;

public sealed class TicketsRealtimeClient : IAsyncDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly TokenStore _tokenStore;
    private HubConnection? _connection;
    private bool _started;

    public event Func<TicketLiveUpdateDto, Task>? OnTicketChanged;

    public TicketsRealtimeClient(NavigationManager navigationManager, TokenStore tokenStore)
    {
        _navigationManager = navigationManager;
        _tokenStore = tokenStore;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_started)
        {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/hubs/tickets"), options =>
            {
                options.AccessTokenProvider = async () =>
                    await _tokenStore.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<TicketLiveUpdateDto>("TicketChanged", async dto =>
        {
            if (OnTicketChanged is not null)
            {
                await OnTicketChanged(dto);
            }
        });

        await _connection.StartAsync(ct);
        await _connection.SendAsync("JoinDashboard", ct);
        _started = true;
    }

    public async Task JoinTicketAsync(int ticketId, CancellationToken ct = default)
    {
        if (_connection is not null && _started)
        {
            await _connection.SendAsync("JoinTicket", ticketId.ToString(), ct);
        }
    }

    public async Task LeaveTicketAsync(int ticketId, CancellationToken ct = default)
    {
        if (_connection is not null && _started)
        {
            await _connection.SendAsync("LeaveTicket", ticketId.ToString(), ct);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
using System.Threading.Tasks;
using Bitar.Services;
using Microsoft.AspNetCore.SignalR;

namespace Bitar.Hubs
{
    public class TickerHub : Hub
    {
        private readonly TickerService _ticker;

        public TickerHub(TickerService ticker)
        {
            _ticker = ticker;
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("TickersUpdated", _ticker.Tickers);
            await base.OnConnectedAsync();
        }
    }
}
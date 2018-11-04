using System.Threading.Tasks;
using Bitar.Services;
using Microsoft.AspNetCore.SignalR;

namespace Bitar.Hubs
{
    public class StockHub : Hub
    {
        private readonly StockService _stock;

        public StockHub(StockService stock)
        {
            _stock = stock;
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("StocksUpdated", _stock.Stocks);
            await base.OnConnectedAsync();
        }
    }
}
using System.Threading.Tasks;
using Bitar.Services;
using Microsoft.AspNetCore.SignalR;

namespace Bitar.Hubs
{
    public class CurrencyHub : Hub
    {
        private readonly CurrencyService _currency;

        public CurrencyHub(CurrencyService currency)
        {
            _currency = currency;
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("currenciesUpdated", _currency.Currencies);
            await base.OnConnectedAsync();
        }
    }
}
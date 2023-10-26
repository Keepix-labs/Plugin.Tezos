using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Tezos.src.Services
{
    public class TezosService
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:8732";

        public TezosService()
        {
            _client = new HttpClient();
        }

        public async Task<string> GetBalanceAsync(string accountAddress)
        {
            string endpoint = $"/chains/main/blocks/head/context/contracts/{accountAddress}/balance";
            HttpResponseMessage response = await _client.GetAsync(BaseUrl + endpoint);

            if (response.IsSuccessStatusCode)
            {
                string balance = await response.Content.ReadAsStringAsync();
                return balance;
            }
            else
            {
                throw new Exception($"Error when trying to get balance: {response.ReasonPhrase}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services
{
    public class AuctionServiceHttpClient
    {
        private readonly HttpClient _httpclient;
        private readonly IConfiguration _config;

        public AuctionServiceHttpClient(HttpClient httpclient, IConfiguration config)
        {
            _httpclient = httpclient;
            _config = config;
        }

        public async Task<List<Item>> GetItemsForSearchDb()
        {
            var lastUpdated = await DB.Find<Item, string>()
            .Sort(x => x.Descending(x => x.UpdateAt))
            .Project(x => x.UpdateAt.ToString())
            .ExecuteFirstAsync();

            return await _httpclient.GetFromJsonAsync<List<Item>>(_config["AuctionServiceUrl"] + "/api/auctions?date=" + lastUpdated);
        }
    }
}
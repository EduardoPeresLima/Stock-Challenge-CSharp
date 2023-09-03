using System;
using System.Net;
using Newtonsoft.Json;
using RestSharp;

namespace stock_monitoring
{
    internal class APIService
    {
        const string SERVER_URL = "https://brapi.dev/api/quote/";
        RestClient client;
        public APIService()
        {
            this.client = new RestClient(SERVER_URL);
        }

        public StockData GetStockData(string stock)
        {
            string getParams = stock + "?range=1d&interval=1m&fundamental=true&dividends=false";
            var request = new RestRequest(getParams);
            var response = this.client.Execute(request);

            StockData result = new StockData();
            if (response.IsSuccessful)
            {
                string rawResponse = response.Content;
                result = JsonConvert.DeserializeObject<StockData>(rawResponse);
            }
            result.statusCode = response.StatusCode;
            result.errorMessage = response.ErrorMessage;
            return result;
        }

        //Classes to manage the JSON data got from the API
        public class StockData
        {
            public Result[] results { get; set; }
            public DateTime requestedAt { get; set; }
            public string took { get; set; }
            public string errorMessage { get; set; }
            public HttpStatusCode statusCode { get; set; }
        }

        public class Result
        {
            public string symbol { get; set; }
            public string currency { get; set; }
            public Historicaldataprice[] historicalDataPrice { get; set; }
        }

        public class Historicaldataprice
        {
            public int date { get; set; }
            public float? open { get; set; }
            public float? high { get; set; }
            public float? low { get; set; }
            public float? close { get; set; }
            public int? volume { get; set; }
            public object adjustedClose { get; set; }
        }
    }
}

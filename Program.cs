using System;
using System.Net;

namespace stock_monitoring
{
    internal class Program
    {
        public enum ValidArgsErrorCode
        {
            OK,
            InvalidArgumentLength,
            SellValueIsNaN,
            BuyValueIsNaN,
            BuyValueGreaterOrEqualSellValue,
            StockNotFound,
            APIError
        }
        static void Main(string[] args)
        {
            RealMain(args);
            //TestMain(args);


        }
        public static void RealMain(string[] args)
        {
            EmailManager emailManager = new EmailManager();
            APIService service = new APIService();
            ValidArgsErrorCode errorCode = CheckValidArgs(service, args);
            if (errorCode != ValidArgsErrorCode.OK)
            {
                switch (errorCode)
                {
                    case ValidArgsErrorCode.InvalidArgumentLength:
                        string argsGiven = $"Arguments given({args.Length}): ";
                        foreach (string arg in args) argsGiven += $"'{arg}' ";
                        Console.WriteLine("Need 3 arguments: 'stock', 'value to sell', 'value to buy'\n" + argsGiven + ".\n");
                        break;
                    case ValidArgsErrorCode.SellValueIsNaN:
                        Console.WriteLine($"Value to sell '{args[1]}' is not a valid number.");
                        break;
                    case ValidArgsErrorCode.BuyValueIsNaN:
                        Console.WriteLine($"Value to buy '{args[2]}' is not a valid number.");
                        break;
                    case ValidArgsErrorCode.BuyValueGreaterOrEqualSellValue:
                        Console.WriteLine($"Given 'Value to buy' is greater than 'Value to sell': {args[2]} > {args[1]}. Do you want to lose money?");
                        break;
                    case ValidArgsErrorCode.StockNotFound:
                        Console.WriteLine($"Stock '{args[0]}' not found on API.");
                        break;
                    case ValidArgsErrorCode.APIError:
                        Console.WriteLine($"An error occurred when trying to access the API.");
                        break;
                }
                Console.ReadKey();
                return;
            }
            string stock = args[0];
            float sellValue = float.Parse(args[1]), buyValue = float.Parse(args[2]);
            Console.WriteLine($"Monitoring stock '{stock}'!");
            Console.WriteLine($"Value to Sell: '{sellValue}'");
            Console.WriteLine($"Value to Buy: '{buyValue}'\n");
            int currentDataId = -1;
            APIService.StockData allStockData = null;

            while (true)
            {
                allStockData = service.GetStockData(stock);
                if (allStockData != null)
                {
                    var generalData = allStockData.results[0];
                    if (currentDataId == -1)
                        currentDataId = (generalData.historicalDataPrice.Length - 1) - 15;
                    string symbol = generalData.symbol;
                    string currency = generalData.currency;
                    var stockData = generalData.historicalDataPrice[currentDataId];
                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    dateTime = dateTime.AddSeconds(stockData.date).ToLocalTime();

                    Console.WriteLine(dateTime + ": " + symbol + " " + stockData.close + currency);
                    if (stockData.close.HasValue) //Sometimes this value is null
                    {
                        float stockCurrentValue = stockData.close.Value;
                        if (stockData.close <= buyValue)
                        {
                            Console.WriteLine("\tTime to Buy!");
                            Console.WriteLine("\tSending an email...");
                            Exception e = emailManager.SendEmail(emailManager.emailTemplates.buyAlert, stock, stockCurrentValue, buyValue);
                            if (e == null)
                                Console.WriteLine("\tEmail sent successfully!");
                            else
                                Console.WriteLine($"\tEmail not sent.\n Error: {e.Message}");
                        }
                        else if (stockData.close >= sellValue)
                        {
                            Console.WriteLine("\tTime to Sell!");
                            Console.Write("\tSending an email...   ");
                            Exception e = emailManager.SendEmail(emailManager.emailTemplates.sellAlert, stock, stockCurrentValue, sellValue);
                            if (e == null)
                                Console.WriteLine("\tEmail sent successfully!");
                            else
                                Console.WriteLine($"\tEmail not sent.\n Error: {e.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error on reading data from API");
                }

                System.Threading.Thread.Sleep(60000); //a minute
                currentDataId++;
            }
        }
        public static void TestMain(string[] args)
        {
            APIService service = new APIService();
            string stock = args[0];
            float sellValue = float.Parse(args[1]), buyValue = float.Parse(args[2]);


            int minuteMod15 = 0;
            int currentDataId = -1;
            APIService.StockData allStockData = null;
            while (true)
            {
                allStockData = service.GetStockData(stock);
                Console.Clear();
                Console.WriteLine(((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds());
                if (allStockData != null)
                {
                    string symbol = allStockData.results[0].symbol;
                    int len = allStockData.results[0].historicalDataPrice.Length;
                    Console.WriteLine(len);
                    for (int i = len - 1, j = 0; j < 20; i--, j++)
                    {
                        var stockData = allStockData.results[0].historicalDataPrice[i];

                        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                        dateTime = dateTime.AddSeconds(stockData.date).ToLocalTime();
                        Console.WriteLine(stockData.date + " " + dateTime + ": " + symbol + " " + stockData.close);
                    }

                }
                else
                {
                    Console.WriteLine("Error on reading data from API");
                }

                System.Threading.Thread.Sleep(10000);
                currentDataId++;
            }
        }
        public static ValidArgsErrorCode CheckValidArgs(APIService service, string[] args)
        {
            if (args.Length != 3) return ValidArgsErrorCode.InvalidArgumentLength;
            string strStock = args[0], strSellValue = args[1], strBuyValue = args[2];
            float sellValue, buyValue;
            if (!float.TryParse(strSellValue, out sellValue)) return ValidArgsErrorCode.SellValueIsNaN;
            if (!float.TryParse(strBuyValue, out buyValue)) return ValidArgsErrorCode.BuyValueIsNaN;
            if (buyValue >= sellValue) return ValidArgsErrorCode.BuyValueGreaterOrEqualSellValue;
            var tryGetStockData = service.GetStockData(strStock);
            if (tryGetStockData.statusCode == HttpStatusCode.NotFound) return ValidArgsErrorCode.StockNotFound;
            else if (tryGetStockData.statusCode != HttpStatusCode.OK) return ValidArgsErrorCode.APIError;

            return ValidArgsErrorCode.OK;
        }
        public void TestEmail()
        {
            EmailManager emailManager = new EmailManager();
            var emailTemplates = emailManager.emailTemplates;
            Console.WriteLine("Sending 'Sell Alert' e-mail");
            emailManager.SendEmail(emailTemplates.sellAlert, "PETR4", 50.00f, 30.00f);
            //emailManager.SendEmail(emailTemplates.buyAlert, "IBOVESPA", 15.00f, 20.00f);
        }
    }
}

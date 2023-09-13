using Newtonsoft.Json;
using System.Net;

namespace stock_monitoring
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Check if configuration files exist and are valid
            string path = Directory.GetCurrentDirectory();
            string emailConfigPath = path + @"\Configurations\email-configuration.json";
            string emailTemplatesPath = path + @"\Configurations\email-templates.json";
            ValidConfigJsons errorCodeSetup = CheckSetups(emailConfigPath, emailTemplatesPath);
            if (errorCodeSetup != ValidConfigJsons.OK)
            {
                switch (errorCodeSetup)
                {
                    case ValidConfigJsons.ConfigPathNotFound:
                        Console.WriteLine($"Email Config path not found: {emailConfigPath}.");
                        break;
                    case ValidConfigJsons.CouldntReadConfig:
                        Console.WriteLine($"Couldn't read Email Config from path: {emailConfigPath}");
                        break;
                    case ValidConfigJsons.TemplatesPathNotFound:
                        Console.WriteLine($"Email Templates path not found: {emailTemplatesPath}.");
                        break;
                    case ValidConfigJsons.CouldntReadTemplates:
                        Console.WriteLine($"Couldn't read Email Templates from path: {emailTemplatesPath}.");
                        break;
                }
                return;
            }
            
            //Setup variables
            APIService service = new APIService(); ;
            EmailManager emailManager = new EmailManager(emailConfigPath, emailTemplatesPath);
            
            //Check if arguments given to the program are valid
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
                return;
            }
            
            
            //Program Start
            string stock = args[0];
            float sellValue = float.Parse(args[1]), buyValue = float.Parse(args[2]);
            Console.WriteLine($"Monitoring stock '{stock}'!");
            Console.WriteLine($"Value to Sell: '{sellValue}'");
            Console.WriteLine($"Value to Buy: '{buyValue}'\n");
            int currentDataId = -1;

            while (true)
            {
                APIService.StockData allStockData = service.GetStockData(stock);
                if (allStockData != null)
                {
                    var generalData = allStockData.results[0];
                    if (currentDataId == -1)
                    {
                        currentDataId = (generalData.historicalDataPrice.Length - 1) - 15;
                    }
                        
                    string symbol = generalData.symbol;
                    string currency = generalData.currency;
                    var stockData = generalData.historicalDataPrice[currentDataId];
                    DateTime dateTime = GetDateTimeFromTimestamp(stockData.date);

                    Console.WriteLine(dateTime + ": " + symbol + " " + stockData.close + currency);
                    if (stockData.close.HasValue) //Sometimes this value is null on the API
                    {
                        float stockCurrentValue = stockData.close.Value;
                        if (stockData.close <= buyValue)
                        {
                            Console.WriteLine("\tTime to Buy!");
                            Console.WriteLine("\tSending an email...");
                            Exception e = emailManager.SendEmail(emailManager.emailTemplates.buyAlert, stock, stockCurrentValue, buyValue);
                            
                            Console.WriteLine(e == null ? "\tEmail sent successfully!" : $"\tEmail not sent.\n Error: {e.Message}");
                        }
                        else if (stockData.close >= sellValue)
                        {
                            Console.WriteLine("\tTime to Sell!");
                            Console.Write("\tSending an email...   ");
                            Exception e = emailManager.SendEmail(emailManager.emailTemplates.sellAlert, stock, stockCurrentValue, sellValue);

                            Console.WriteLine(e == null ? "\tEmail sent successfully!" : $"\tEmail not sent.\n Error: {e.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error on reading data from API");
                }

                Thread.Sleep(60000); //a minute
                currentDataId++;
            }
        }
        //Auxiliar function
        public static DateTime GetDateTimeFromTimestamp(long timestamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dateTime.AddSeconds(timestamp).ToLocalTime();
        }
        //Enums for check setup files and arguments before starting the program
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
        public static ValidConfigJsons CheckSetups(string emailConfigPath, string emailTemplatesPath)
        {
            if (!File.Exists(emailConfigPath)) return ValidConfigJsons.ConfigPathNotFound;
            if (!File.Exists(emailTemplatesPath)) return ValidConfigJsons.TemplatesPathNotFound;
            var emailConfig = JsonConvert.DeserializeObject<EmailManager.EmailSenderConfiguration>(File.ReadAllText(emailConfigPath));
            if (emailConfig == null) return ValidConfigJsons.CouldntReadConfig;

            var emailTemplates = JsonConvert.DeserializeObject<EmailManager.EmailTemplates>(File.ReadAllText(emailTemplatesPath));
            if (emailTemplates == null) return ValidConfigJsons.CouldntReadTemplates;
            return ValidConfigJsons.OK;
        }
        
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
        public enum ValidConfigJsons
        {
            OK,
            ConfigPathNotFound,
            CouldntReadConfig,
            TemplatesPathNotFound,
            CouldntReadTemplates
        }
    }
}
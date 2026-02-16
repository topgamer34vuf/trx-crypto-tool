using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CryptoPortfolioSimulator
{
    #region Models

    public class CryptoAsset
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public decimal CurrentPrice { get; set; }

        public CryptoAsset() { }

        public CryptoAsset(string symbol, string name, decimal price)
        {
            Symbol = symbol;
            Name = name;
            CurrentPrice = price;
        }

        public override string ToString()
        {
            return $"{Symbol} ({Name}) - ${CurrentPrice}";
        }
    }

    public class PortfolioItem
    {
        public CryptoAsset Asset { get; set; }
        public decimal Quantity { get; set; }

        public decimal Value => Asset.CurrentPrice * Quantity;

        public override string ToString()
        {
            return $"{Asset.Symbol} | Qty: {Quantity} | Value: ${Value:F2}";
        }
    }

    public class Portfolio
    {
        private List<PortfolioItem> _items = new List<PortfolioItem>();

        public IReadOnlyList<PortfolioItem> Items => _items;

        public void AddAsset(CryptoAsset asset, decimal qty)
        {
            var existing = _items.FirstOrDefault(x => x.Asset.Symbol == asset.Symbol);
            if (existing != null)
            {
                existing.Quantity += qty;
            }
            else
            {
                _items.Add(new PortfolioItem
                {
                    Asset = asset,
                    Quantity = qty
                });
            }
        }

        public void RemoveAsset(string symbol, decimal qty)
        {
            var existing = _items.FirstOrDefault(x => x.Asset.Symbol == symbol);
            if (existing == null) return;

            existing.Quantity -= qty;
            if (existing.Quantity <= 0)
                _items.Remove(existing);
        }

        public decimal TotalValue()
        {
            return _items.Sum(x => x.Value);
        }

        public void Print()
        {
            Console.WriteLine("==== PORTFOLIO ====");
            foreach (var item in _items)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine($"TOTAL: ${TotalValue():F2}");
            Console.WriteLine("===================");
        }
    }

    #endregion

    #region Services

    public static class MarketDataService
    {
        private static Random _random = new Random();

        public static List<CryptoAsset> GenerateMarket()
        {
            return new List<CryptoAsset>
            {
                new CryptoAsset("BTC","Bitcoin", RandomPrice(30000,60000)),
                new CryptoAsset("ETH","Ethereum", RandomPrice(1500,4000)),
                new CryptoAsset("TRX","Tron", RandomPrice(0.05m,0.20m)),
                new CryptoAsset("BNB","Binance Coin", RandomPrice(200,600)),
                new CryptoAsset("SOL","Solana", RandomPrice(20,200)),
                new CryptoAsset("ADA","Cardano", RandomPrice(0.2m,2.0m)),
                new CryptoAsset("XRP","Ripple", RandomPrice(0.3m,1.5m)),
                new CryptoAsset("DOGE","Dogecoin", RandomPrice(0.05m,0.5m))
            };
        }

        private static decimal RandomPrice(decimal min, decimal max)
        {
            return min + (decimal)_random.NextDouble() * (max - min);
        }

        public static void SimulateMarketFluctuation(List<CryptoAsset> market)
        {
            foreach (var asset in market)
            {
                var change = (decimal)(_random.NextDouble() - 0.5) * 0.1m;
                asset.CurrentPrice += asset.CurrentPrice * change;
                if (asset.CurrentPrice < 0.0001m)
                    asset.CurrentPrice = 0.0001m;
            }
        }
    }

    public static class PortfolioAnalytics
    {
        public static void PrintAllocation(Portfolio portfolio)
        {
            var total = portfolio.TotalValue();
            if (total == 0)
            {
                Console.WriteLine("Portfolio empty.");
                return;
            }

            Console.WriteLine("=== Allocation ===");
            foreach (var item in portfolio.Items)
            {
                var percent = item.Value / total * 100;
                Console.WriteLine($"{item.Asset.Symbol}: {percent:F2}%");
            }
        }

        public static void PrintTopHolding(Portfolio portfolio)
        {
            if (!portfolio.Items.Any())
            {
                Console.WriteLine("Portfolio empty.");
                return;
            }

            var top = portfolio.Items.OrderByDescending(x => x.Value).First();
            Console.WriteLine($"Top Holding: {top.Asset.Symbol} - ${top.Value:F2}");
        }
    }

    public static class FileService
    {
        public static void SavePortfolio(string path, Portfolio portfolio)
        {
            var json = JsonSerializer.Serialize(portfolio.Items);
            File.WriteAllText(path, json);
        }

        public static Portfolio LoadPortfolio(string path, List<CryptoAsset> market)
        {
            var portfolio = new Portfolio();
            if (!File.Exists(path)) return portfolio;

            var json = File.ReadAllText(path);
            var items = JsonSerializer.Deserialize<List<PortfolioItem>>(json);

            foreach (var item in items)
            {
                var asset = market.FirstOrDefault(x => x.Symbol == item.Asset.Symbol);
                if (asset != null)
                    portfolio.AddAsset(asset, item.Quantity);
            }

            return portfolio;
        }
    }

    #endregion

    #region UI

    public static class Menu
    {
        public static void Show()
        {
            Console.WriteLine();
            Console.WriteLine("1. View Market");
            Console.WriteLine("2. Buy Asset");
            Console.WriteLine("3. Sell Asset");
            Console.WriteLine("4. View Portfolio");
            Console.WriteLine("5. Allocation");
            Console.WriteLine("6. Top Holding");
            Console.WriteLine("7. Simulate Market Move");
            Console.WriteLine("8. Save Portfolio");
            Console.WriteLine("9. Load Portfolio");
            Console.WriteLine("0. Exit");
            Console.Write("Select: ");
        }
    }

    #endregion

    class Program
    {
        static void Main(string[] args)
        {
            var market = MarketDataService.GenerateMarket();
            var portfolio = new Portfolio();
            string savePath = "portfolio.json";

            bool running = true;

            while (running)
            {
                Menu.Show();
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        PrintMarket(market);
                        break;

                    case "2":
                        BuyAsset(market, portfolio);
                        break;

                    case "3":
                        SellAsset(portfolio);
                        break;

                    case "4":
                        portfolio.Print();
                        break;

                    case "5":
                        PortfolioAnalytics.PrintAllocation(portfolio);
                        break;

                    case "6":
                        PortfolioAnalytics.PrintTopHolding(portfolio);
                        break;

                    case "7":
                        MarketDataService.SimulateMarketFluctuation(market);
                        Console.WriteLine("Market updated.");
                        break;

                    case "8":
                        FileService.SavePortfolio(savePath, portfolio);
                        Console.WriteLine("Saved.");
                        break;

                    case "9":
                        portfolio = FileService.LoadPortfolio(savePath, market);
                        Console.WriteLine("Loaded.");
                        break;

                    case "0":
                        running = false;
                        break;
                }
            }
        }

        static void PrintMarket(List<CryptoAsset> market)
        {
            Console.WriteLine("=== MARKET ===");
            foreach (var asset in market)
            {
                Console.WriteLine(asset);
            }
        }

        static void BuyAsset(List<CryptoAsset> market, Portfolio portfolio)
        {
            Console.Write("Symbol: ");
            var symbol = Console.ReadLine()?.ToUpper();

            var asset = market.FirstOrDefault(x => x.Symbol == symbol);
            if (asset == null)
            {
                Console.WriteLine("Not found.");
                return;
            }

            Console.Write("Quantity: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal qty))
            {
                portfolio.AddAsset(asset, qty);
                Console.WriteLine("Added.");
            }
        }

        static void SellAsset(Portfolio portfolio)
        {
            Console.Write("Symbol: ");
            var symbol = Console.ReadLine()?.ToUpper();

            Console.Write("Quantity: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal qty))
            {
                portfolio.RemoveAsset(symbol, qty);
                Console.WriteLine("Removed.");
            }
        }
    }
}

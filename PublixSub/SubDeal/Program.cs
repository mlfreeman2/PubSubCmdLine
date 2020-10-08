using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PublixDotCom.Promotions;
using PublixDotCom.StoreInfo;
using PublixDotCom.Util;


namespace PublixSub
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]) || !Regex.IsMatch(args[0], @"^\d+$"))
            {
                Console.WriteLine("Required: ZIP Code");
            }
            
            if (!Regex.IsMatch(args[0], @"^\d+$") && !Regex.IsMatch(args[0], @"^\d+-\d+$"))
            {
                Console.WriteLine("Required: ZIP Code");
            }

            var stores = await StoreList.Fetch(args[0]);
            
            if (stores == null || !stores.Any())
            {
                throw new InvalidOperationException($"No stores found for zip code {args[0]}");
            }

            var ad = await Welcome.Fetch(stores[0]);

            var subs = ad.Rollovers.Where(a => a.Title.Contains("Whole Sub")).ToList();

            Console.WriteLine($"Subs For Sale In {args[0]} From {ad.SaleStartDate.ToShortDateString()} To {ad.SaleEndDate.ToShortDateString()}");
            Console.WriteLine("--------------------------------------------------");
            foreach (var sub in subs)
            {
                Console.WriteLine($"{sub.Title} - {sub.Deal}");
            }
        }
    }
}

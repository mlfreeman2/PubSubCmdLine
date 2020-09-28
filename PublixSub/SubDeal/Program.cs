using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using PublixDotCom.StoreInfo;
using PublixSub.Util;

namespace PublixSub
{
    class Program
    {
        private const string PublixAdQuery = @"
query Promotion($promotionCode: ID, $previewHash: String, $promotionTypeID: Int, $require: String, $nuepOpen: Boolean) {
    promotion(code: $promotionCode, imageWidth: 1400, previewHash: $previewHash, promotionTypeID: $promotionTypeID, require: $require) {
        id
        title
        displayOrder
        saleStartDateString
        saleEndDateString
        postStartDateString
        previewPostStartDateString
        code
        rollovers(previewHash: $previewHash, require: $require) {
            id
            title
            deal
            isCoupon
            buyOnlineLinkURL
        }
        pages(imageWidth: 1400, previewHash: $previewHash, require: $require, nuepOpen: $nuepOpen) {
            id
            imageURL(previewHash: $previewHash,require: $require)
            order
        }
    }
}";

        public static async Task Main(string[] args)
        {
            var storeInfoUrl = "https://services.publix.com/api/v1/storelocation?types=R,G,H,N,S&option=&count=15&includeOpenAndCloseDates=true&zipCode={0}";
            var promoGraphQLServer = "https://graphql-cdn-slplatform.liquidus.net/";
            
            if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]) || !Regex.IsMatch(args[0], @"^\d+$"))
            {
                Console.WriteLine("Required: ZIP Code");
            }
            
            if (!Regex.IsMatch(args[0], @"^\d+$") && !Regex.IsMatch(args[0], @"^\d+-\d+$"))
            {
                Console.WriteLine("Required: ZIP Code");
            }

            var storesResponse = await WebClient.GetString(string.Format(storeInfoUrl, args[0]), "application/json");

            var storeList = JsonConvert.DeserializeObject<StoreList>(storesResponse, PublixDotCom.StoreInfo.Converter.Settings);
            if (storeList == null || !storeList.Stores.Any()) 
            {
                Console.Error.WriteLine($"No Publix stores near {args[0]}. :(");
                Environment.Exit(1);
            }

            var storeRef = storeList.Stores.First(a => string.IsNullOrWhiteSpace(a.Status)).Key;

            var graphQLClient = new GraphQLHttpClient(promoGraphQLServer, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("campaignid", "80db0669da079dc6");
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("storeref", storeRef);
            graphQLClient.HttpClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue() { NoCache = true };
            graphQLClient.HttpClient.DefaultRequestHeaders.Pragma.ParseAdd("No-Cache");
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("authority", new Uri(promoGraphQLServer).Host);
            graphQLClient.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(WebClient.UserAgent);

            var graphQLResponse = await graphQLClient.SendQueryAsync<PublixDotCom.Promotions.Welcome>(new GraphQLRequest {
                Query = PublixAdQuery,
                OperationName = "Promotion",
                Variables = new {
                    sort = "",
                    preload = 3,
                    disablesneakpeekhero = false,
                    countryid = 1,
                    languageid = 1,
                    env = "undefined",
                    storeref = storeRef,
                    storeid = "undefined",
                    campaignid = "80db0669da079dc6",
                    require = "",
                    nuepOpen = false
                }
            });
            var ad = graphQLResponse.Data;

            var startDate =  DateTime.ParseExact(ad.Promotion.SaleStartDateString, "MMM dd, yyyy hh:mm:ss tt", CultureInfo.CurrentCulture);
            var endDate =  DateTime.ParseExact(ad.Promotion.SaleEndDateString, "MMM dd, yyyy hh:mm:ss tt", CultureInfo.CurrentCulture);

            var subs = ad.Promotion.Rollovers.Where(a => a.Title.Contains("Whole Sub")).ToList();

            Console.WriteLine($"Subs For Sale In {args[0]} From {startDate.ToShortDateString()} To {endDate.ToShortDateString()}");
            Console.WriteLine("--------------------------------------------------");
            foreach (var sub in subs)
            {
                Console.WriteLine($"{sub.Title} - {sub.Deal}");
            }
        }
    }
}

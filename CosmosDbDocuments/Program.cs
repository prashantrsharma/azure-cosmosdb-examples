using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using Microsoft.Azure.Documents.Linq;
using System.Diagnostics;

namespace CosmosDbDocuments
{
    class Program
    {

        public static readonly Uri collectionUri = UriFactory.CreateDocumentCollectionUri("VinDecode","VehicleManufacturer");
        private static readonly string cosmosDbEndpoint = ConfigurationManager.AppSettings["CosmosDbEndpoint"].ToString();
        private static readonly string cosmosDbMasterKey = ConfigurationManager.AppSettings["CosmosDbMasterKey"].ToString();

        static void Main(string[] args)
        {
            
            Task.Run(async () =>
            {
                Debugger.Break();

                using (var client = new DocumentClient(new Uri(cosmosDbEndpoint), cosmosDbMasterKey))
                {
                    // Do actions
                    await CreateDocumentsAsync(client);

                    //SQL
                    QueryDocumentsWithSql(client);
                    await QueryDocumentsWithPaging(client);
                    QueryDocumentsWithLinq(client);

                }
            }).Wait();

        }

        public async static Task CreateDocumentsAsync(DocumentClient client)
        {
            Console.WriteLine();
            Console.WriteLine(">>>> Create Document >>>>");

            dynamic alienVehicleManufacturer = new
            {
                VIN12310 = "VIN12310",
                YearMakeShort = "2050AlienM",
                YearMakeLong = "2050AlienMade",
                VINModelDecode = "567",
                Year = 2050,
                Make= "AlienMade"
            };

            Document alienDocument = await CreateDocumentAsync(client, alienVehicleManufacturer);
            Console.WriteLine($"Created Document {alienDocument.Id} from dynamic object");
            Console.WriteLine();

            var extraterrestialDocument = @"
            {
                ""VIN12310"": ""ETV1"",
                ""YearMakeShort"": ""2060ET"",
                ""YearMakeLong"": ""2060ETV"",
                ""VINModelDecode"": 45,
                ""Year"": 2060,
                ""Make"": ""ET"",
            }";

            Document etDocument = await CreateDocumentAsync(client, JsonConvert.DeserializeObject(extraterrestialDocument));
            Console.WriteLine($"Created Document {etDocument.Id} from JSON String");
            Console.WriteLine();

            var vehicleData = new VehicleManufacturer
            {
                VIN12310 = "VINMARS",
                YearMakeShort = "2050MARM",
                YearMakeLong = "2050MARSM",
                VINModelDecode = "56",
                Year = 2070,
                Make = "MarsMade"
            };

            Document marsVehicle = await CreateDocumentAsync(client,vehicleData);
            Console.WriteLine($"Created Document {marsVehicle.Id} from Poco");
            Console.WriteLine();
        }

        public async static Task<Document> CreateDocumentAsync(DocumentClient client, object documentObject)
        {
            var result =   await client.CreateDocumentAsync(collectionUri, documentObject);
            var document = result.Resource;
            Console.WriteLine($"Created new document: {document.Id}");
            Console.WriteLine(document);
            return result;
        }

        public static void QueryDocumentsWithSql(DocumentClient client)
        {
            Console.WriteLine();
            Console.WriteLine(">>> Query Documents (SQL) <<<");
            Console.WriteLine();

            Console.WriteLine("Querying for new vehicle manufacturer documents (SQL)");
            var sql = "SELECT *  FROM c  WHERE c.Year > 2050";
            var options = new FeedOptions { EnableCrossPartitionQuery = true};

            //Query  for dynamic objects
            var documents = client.CreateDocumentQuery(collectionUri,sql,options).ToList();
            Console.WriteLine($"Found {documents.Count} new documents");

            foreach (var document in documents)
            {
                Console.WriteLine($" Id: {document.id};");

                //Use Poco
                var vehicleManufacturer = JsonConvert.DeserializeObject<VehicleManufacturer>(document.ToString());
                Console.WriteLine($" Make: {vehicleManufacturer.Make}");
                Console.WriteLine();
            }

            Console.WriteLine("Querying for all documents (SQL)");
            sql = "SELECT * FROM c";
            documents = client.CreateDocumentQuery(collectionUri, sql, options).ToList();

            Console.WriteLine($"Found {documents.Count} documents");
            foreach (var document in documents)
            {
                Console.WriteLine($" Id: {document.id}; ");
            }
            Console.WriteLine();
        }

        private async static Task QueryDocumentsWithPaging(DocumentClient client)
        {
            Console.WriteLine();
            Console.WriteLine(">>> Query Documents (paged results) <<<");
            Console.WriteLine();

            Console.WriteLine("Querying for all documents");
            var sql = "SELECT * FROM c";
            var options = new FeedOptions { EnableCrossPartitionQuery = true };

            var query = client
                .CreateDocumentQuery(collectionUri, sql, options)
                .AsDocumentQuery();

            while (query.HasMoreResults)
            {
                var documents = await query.ExecuteNextAsync();
                foreach (var document in documents)
                {
                    Console.WriteLine($" Id: {document.id};");
                }
            }
            Console.WriteLine();
        }

        private static void QueryDocumentsWithLinq(DocumentClient client)
        {
            Console.WriteLine();
            Console.WriteLine(">>> Query Documents (LINQ) <<<");
            Console.WriteLine();

            var options = new FeedOptions { EnableCrossPartitionQuery = true };

            Console.WriteLine("Querying for Cars with Make ACURA (LINQ)");
            var q =
                from d in client.CreateDocumentQuery<VehicleManufacturer>(collectionUri, options)
                where d.Make == "ACURA"
                select new
                {
                    d.Make,
                    d.Year,
                    d.YearMakeLong
                };

            var documents = q.ToList();

            Console.WriteLine($"Found {documents.Count} for Acura as manufacturer");
            foreach (var document in documents)
            {
                var d = document as dynamic;
                Console.WriteLine($" Make: {d.Make}; Year: {d.Year}");
            }
            Console.WriteLine();
        }

    }

    public  class VehicleManufacturer
    {
        public string VIN12310 { get; set; }
        public string YearMakeShort { get; set; }
        public string YearMakeLong { get; set; }
        public string VINModelDecode { get; set; }
        public int Year { get; set; }
        public string Make { get; set; }
    }
}

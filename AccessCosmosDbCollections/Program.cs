using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace AccessCosmosDbCollections
{
    class Program
    {
        private static readonly System.Uri databaseUri = UriFactory.CreateDatabaseUri("VinDecode");
        private static readonly string cosmosDbEndpoint = ConfigurationManager.AppSettings["CosmosDbEndpoint"].ToString();
        private static readonly string cosmosDbMasterKey = ConfigurationManager.AppSettings["CosmosDbMasterKey"].ToString();

           
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                System.Diagnostics.Debugger.Break();

                using (var client = new DocumentClient(new System.Uri(cosmosDbEndpoint), cosmosDbMasterKey))
                {
                    ViewCollections(client);

                    await CreateCollectionAsync(client, "MyCollection1");
                    await CreateCollectionAsync(client, "MyCollection2", 25000);
                    ViewCollections(client);

                    await DeleteCollectionAsync(client, "MyCollection1");
                    await DeleteCollectionAsync(client, "MyCollection2");
                    ViewCollections(client);

                }
            });
               
        }



        public static void ViewCollections(DocumentClient client)
        {
            Console.WriteLine(">>>> View Collection in VinDecode DB <<<<");

            var collections = client.CreateDocumentCollectionQuery(databaseUri);
            var i = 0;

            foreach (var collection in collections)
            {
                i++;
                Console.WriteLine($" Collection {i}");
                ViewCollection(collection);
            }

            Console.WriteLine($"Total collections in VinDecode DB: {collections.Count()}");
        }

        public static void ViewCollection(DocumentCollection collection)
        {
            Console.WriteLine($" Collection Id : {collection.Id}");
            Console.WriteLine($" Resource Id : {collection.ResourceId}");
            Console.WriteLine($" Self Link : {collection.SelfLink}");
            Console.WriteLine($" E-Tag : {collection.ETag}");
            Console.WriteLine($" Timestamp : {collection.Timestamp}");
        }

        public static async Task CreateCollectionAsync(
            DocumentClient client, string collectionId,
            int reservedRUs = 1000,
            string partitionKey = "/partitionKey")
        {
            Console.WriteLine($">>>> Create Collection {collectionId} in VinDecode Db <<<<");
            Console.WriteLine($" Throughput : {reservedRUs} RU/sec");
            Console.WriteLine($" Partition Key :{partitionKey}");

            var partitionKeyDefinition = new PartitionKeyDefinition();
            partitionKeyDefinition.Paths.Add(partitionKey);

            var collectionDefinition = new DocumentCollection()
            {
                Id = collectionId,
                PartitionKey = partitionKeyDefinition
            };

            var options = new RequestOptions { OfferThroughput = reservedRUs };

            var result = await client.CreateDocumentCollectionAsync(databaseUri, collectionDefinition, options);
            var collection = result.Resource;

            Console.WriteLine(">>> Created New Collection <<<<");
            ViewCollection(collection);

        }

        public async static Task DeleteCollectionAsync(DocumentClient client, string collectionId)
        {
            Console.WriteLine($"Delete Collection {collectionId} in VinDecode Db");

            var collectionUri = UriFactory.CreateDocumentCollectionUri("VinDecode", collectionId);
            await client.DeleteDocumentCollectionAsync(collectionUri);

            Console.WriteLine($"Deleted Collection {collectionId} from database VinDecode Db");
        }
    }
}

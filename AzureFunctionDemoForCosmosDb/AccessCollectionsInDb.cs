using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System.Configuration;

namespace AzureFunctionDemoForCosmosDb
{
    public static class AccessCollectionsInDb
    {
        private static readonly System.Uri databaseUri = UriFactory.CreateDatabaseUri("VinDecode");
        private static readonly string cosmosDbEndpoint = ConfigurationManager.AppSettings["CosmosDbEndpoint"].ToString();
        private static readonly string cosmosDbMasterKey = ConfigurationManager.AppSettings["CosmosDbMasterKey"].ToString();

        [FunctionName("AccessCollectionsInDb")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get",Route = null)]HttpRequestMessage req,
            TraceWriter log)
        {
            log.Info("AccessCollectionsInDb HTTP trigger function processed a request.");
            try
            {
                using (var client = new DocumentClient(new System.Uri(cosmosDbEndpoint),cosmosDbMasterKey))
                {
                    
                    ViewCollections(client, log);

                    await CreateCollectionAsync(client,"MyCollection1",log);
                    await CreateCollectionAsync(client, "MyCollection2",log,25000);
                    ViewCollections(client, log);

                    await DeleteCollectionAsync(client, "MyCollection1",log);
                    await DeleteCollectionAsync(client, "MyCollection2", log);
                    ViewCollections(client, log);
                   
                }

                return req.CreateResponse(HttpStatusCode.OK);

            }
            catch (System.Exception ex)
            {
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError,$"We apologize but something went wrong on our end ,Exception message : {ex.Message}");
            }
            finally
            {
                log.Info("AccessCollectionsInDb HTTP trigger function has finished processing a request.");
            }
            

        }


        public static void ViewCollections(DocumentClient client, TraceWriter log)
        {
            log.Info(">>>> View Collection in VinDecode DB <<<<");

            var collections = client.CreateDocumentCollectionQuery(databaseUri);
            var i = 0;

            foreach (var collection in collections)
            {
                i++;
                log.Info($" Collection {i}");
                ViewCollection(collection,log);
            }

            log.Info($"Total collections in VinDecode DB: {collections.Count()}");
        }

        public static void ViewCollection(DocumentCollection collection , TraceWriter log)
        {
            log.Info($" Collection Id : {collection.Id}");
            log.Info($" Resource Id : {collection.ResourceId}");
            log.Info($" Self Link : {collection.SelfLink}");
            log.Info($" E-Tag : {collection.ETag}");
            log.Info($" Timestamp : {collection.Timestamp}");
        }

        public static async Task CreateCollectionAsync(
            DocumentClient client, string collectionId ,
            TraceWriter log,
            int reservedRUs = 1000, 
            string partitionKey = "/partitionKey")
        {
            log.Info($">>>> Create Collection {collectionId} in VinDecode Db <<<<");
            log.Info($" Throughput : {reservedRUs} RU/sec");
            log.Info($" Partition Key :{partitionKey}");
            
            var partitionKeyDefinition = new PartitionKeyDefinition();
            partitionKeyDefinition.Paths.Add(partitionKey);

            var collectionDefinition = new DocumentCollection()
            {
                Id = collectionId,
                PartitionKey = partitionKeyDefinition
            };

            var options = new RequestOptions { OfferThroughput = reservedRUs };

            var result = await client.CreateDocumentCollectionAsync(databaseUri, collectionDefinition,options);
            var collection = result.Resource;

            log.Info(">>> Created New Collection <<<<");
            ViewCollection(collection,log);

        }

        public async static Task DeleteCollectionAsync(DocumentClient client, string collectionId, TraceWriter log)
        {
            log.Info($"Delete Collection {collectionId} in VinDecode Db");

            var collectionUri = UriFactory.CreateDocumentCollectionUri("VinDecode", collectionId);
            await client.DeleteDocumentCollectionAsync(collectionUri);

            log.Info($"Deleted Collection {collectionId} from database VinDecode Db");
        }
    }
}

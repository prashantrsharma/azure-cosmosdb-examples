using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace DotnetSdkForCosmosDb
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async() =>
            {
                Debugger.Break();

                var endpoint = ConfigurationManager.AppSettings["CosmosDbEndpoint"].ToString();
                var masterKey = ConfigurationManager.AppSettings["CosmosDbMasterKey"].ToString();

                using (var client = new DocumentClient(new Uri(endpoint),masterKey))
                {
                    ViewDatabases(client);

                    await CreateDatabase(client);
                    ViewDatabases(client);

                    await DeleteDatabase(client);
                }

            }).Wait();
        }

        public static void ViewDatabases(DocumentClient client)
        {
            Console.WriteLine();
            Console.WriteLine(">>>> View Databases <<<<");

            var databases = client.CreateDatabaseQuery();

            foreach (var database in databases)
            {
                Console.WriteLine($" Database Id : {database.Id}; Rid {database.ResourceId}");
            }

            Console.WriteLine();
            Console.WriteLine($"Total databases: {databases.Count()}");
        }

        public async static Task CreateDatabase(DocumentClient client)
        {
            Console.WriteLine();
            Console.WriteLine(">>>> Create Database <<<<");

            var databaseDefinition = new Database { Id = "CosmosDBExamples"};
            var result = await client.CreateDatabaseAsync(databaseDefinition);
            var database = result.Resource;

            Console.WriteLine($" Database Id : {database.Id}; Rid {database.ResourceId}");

        }

        public async static Task DeleteDatabase(DocumentClient client)
        {
            Console.WriteLine();
            Console.WriteLine(">>>> Delete Database <<<<");

            var databaseUri = UriFactory.CreateDatabaseUri("CosmosDBExamples");
            var result = await client.DeleteDatabaseAsync(databaseUri);
           
        }
    }
}

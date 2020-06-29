using System.Threading.Tasks;
using ApiTools.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace ApiTools.Services
{
    public interface IQueueService
    {
        Task SetQueue<T>(string referenceName, T data);
    }

    public class QueueService : IQueueService
    {
        private readonly string _azureWebJobsStorage;
        private readonly ITokenService _tokenService;

        public QueueService(ITokenService tokenService, IConfiguration configuration)
        {
            _tokenService = tokenService;
            _azureWebJobsStorage = configuration.GetConnectionString("AzureStorageConnection");
        }

        public async Task SetQueue<T>(string referenceName, T data)
        {
            var storageAccount = CloudStorageAccount.Parse(_azureWebJobsStorage);

            var queueClient = storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(referenceName);
            await queue.CreateIfNotExistsAsync();

            var authorizationToken =
                _tokenService.GenerateToken(
                    _tokenService.GenerateClaims(string.Empty,
                        AuthorizationRoles.AzureFunction
                    )
                );

            await queue.AddMessageAsync(
                new CloudQueueMessage(JsonConvert.SerializeObject(new AzureFunctionRequest<T>
                {
                    Data = data,
                    AuthorizationToken = authorizationToken
                })));
        }
    }
}
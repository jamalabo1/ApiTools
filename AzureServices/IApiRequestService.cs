using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ApiTools.AzureServices
{
    public interface IApiRequestsService
    {
        Task<string> Post<T>(string path, T data);
        Task<HttpResponseMessage> Put<T>(string path, T data);
        Task<string> Get(string path);
        Task<T> Get<T>(string path);

        void SetBearerAuthorizationToken(string authorizationToken);
        void SetAuthorizationToken(string authorizationToken);
    }

    public class ApiRequestsService : IApiRequestsService
    {
        private readonly string _apiUrl;
        private readonly HttpClient _httpClient;

        public ApiRequestsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiUrl = Environment.GetEnvironmentVariable("API_URL");
        }


        public async Task<string> Post<T>(string path, T data)
        {
            var response = await _httpClient.PostAsJsonAsync(ApiPath(path), data);
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public async Task<HttpResponseMessage> Put<T>(string path, T data)
        {
            var response = await _httpClient.PutAsJsonAsync(ApiPath(path), data);
            return response;
        }

        public async Task<string> Get(string path)
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiPath(path));
                if (response == null) return null;
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<T> Get<T>(string path) 
        {
            var value = await Get(path);
            return string.IsNullOrEmpty(value) ? default : JsonConvert.DeserializeObject<T>(value);
        }

        public void SetBearerAuthorizationToken(string authorizationToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authorizationToken);
        }

        public void SetAuthorizationToken(string authorizationToken)
        {
            SetBearerAuthorizationToken(authorizationToken.Replace("Bearer", "").Trim());
        }

        private string ApiPath(string path)
        {
            if (path.FirstOrDefault() == '/') return _apiUrl + path;
            return _apiUrl + "/" + path;
        }
    }
}
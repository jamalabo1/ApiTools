using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;

namespace ApiTools.AzureServices
{
    public interface IApiRequestsService
    {
        Task<HttpResponseMessage> Post<T>(string path, T data);
        Task<HttpResponseMessage> Put<T>(string path, T data);
        Task<HttpResponseMessage> Get(string path);
        Task<T> Get<T>(string path);
        Task<HttpResponseMessage> Patch<T>(string path, JsonPatchDocument<T> data) where T : class;

        void SetBearerAuthorizationToken(string authorizationToken);
        void SetAuthorizationToken(string authorizationToken);
    }

    public static class HttpClientExtension
    {
        public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, string requestUri, T value)
        {
            var content = new ObjectContent<T>(value, new JsonMediaTypeFormatter());
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) {Content = content};

            return client.SendAsync(request);
        }
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


        public async Task<HttpResponseMessage> Post<T>(string path, T data)
        {
            var response = await _httpClient.PostAsJsonAsync(ApiPath(path), data);
            return response;
        }

        public async Task<HttpResponseMessage> Patch<T>(string path, JsonPatchDocument<T> data) where T : class
        {
            var response = await _httpClient.PatchAsJsonAsync(ApiPath(path), data);
            return response;
        }

        public async Task<HttpResponseMessage> Put<T>(string path, T data)
        {
            var response = await _httpClient.PutAsJsonAsync(ApiPath(path), data);
            return response;
        }

        public async Task<HttpResponseMessage> Get(string path)
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiPath(path));
                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<T> Get<T>(string path)
        {
            var value = await (await Get(path)).Content.ReadAsStringAsync();
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
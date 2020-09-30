using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using ApiTools.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serialization;
using RestSharp.Serialization.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ApiTools.AzureServices
{
    public interface IApiRequestsService
    {
        Task<IServiceResponse<T>> Post<T>(string path, T data) where T : class;
        Task<HttpResponseMessage> Put<T>(string path, T data) where T : class;
        Task<HttpResponseMessage> Get(string path, [CanBeNull] object query = null);

        [CanBeNull]
        Task<T> Get<T>(string path, [CanBeNull] object query = null) where T : class;

        IRestResponse Patch<T>(string path,
            T data) where T : class;

        void SetBearerAuthorizationToken(string authorizationToken);
        void SetAuthorizationToken(string authorizationToken);
    }

    public class ApiRequestsService : IApiRequestsService
    {
        private readonly string _apiUrl;
        private readonly RestClient _client;
        private readonly HttpClient _httpClient;


        public ApiRequestsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiUrl = Environment.GetEnvironmentVariable("API_URL")!;
            var client = new RestClient(_apiUrl ?? "");
            _client = client;
            _client.UseSerializer<JsonNetSerializer>();
        }


        public async Task<IServiceResponse<T>> Post<T>(string path, T data) where T : class
        {
            var response = await _httpClient.PostAsJsonAsync(ApiPath(path, null), data);
            var str = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ServiceResponse<T>>(str);
        }

        public async Task<HttpResponseMessage> Put<T>(string path, T data) where T : class
        {
            var response = await _httpClient.PutAsJsonAsync(ApiPath(path, null), data);
            return response;
        }

        public async Task<HttpResponseMessage> Get(string path, object? query = null)
        {
            try
            {
                var getPath = ApiPath(path, query);
                var response = await _httpClient.GetAsync(getPath);
                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<T> Get<T>(string path, object? query = null) where T : class
        {
            var request = await Get(path, query);
            var value = await request.Content.ReadAsStringAsync();
            return !string.IsNullOrEmpty(value) ? JsonConvert.DeserializeObject<T>(value) : null;
        }

        public void SetBearerAuthorizationToken(string authorizationToken)
        {
            _client.Authenticator = new JwtAuthenticator(authorizationToken);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authorizationToken);
        }

        public void SetAuthorizationToken(string authorizationToken)
        {
            SetBearerAuthorizationToken(authorizationToken.Replace("Bearer", "").Trim());
        }


        private class JsonNetSerializer : IRestSerializer
        {
            public string Serialize(object obj) => 
                JsonConvert.SerializeObject(obj);

            public string Serialize(Parameter parameter) => 
                JsonConvert.SerializeObject(parameter.Value);

            public T Deserialize<T>(IRestResponse response) => 
                JsonConvert.DeserializeObject<T>(response.Content);

            public string[] SupportedContentTypes { get; } =
            {
                "application/json", "text/json", "text/x-json", "text/javascript", "*+json"
            };

            public string ContentType { get; set; } = "application/json";

            public DataFormat DataFormat { get; } = DataFormat.Json;
        }
        
        public IRestResponse Patch<T>(string path, T data) where T : class
        {
            // var content = new ObjectContent<string>(JsonConvert.SerializeObject(data), new JsonMediaTypeFormatter());
            // var apiPath = ApiPath(path);
            // var response = await _httpClient.PatchAsync(apiPath, content);

            // return response;
            var resetRequest = new RestRequest(path)
                .AddJsonBody(data);

            
            return _client.Patch<T>(resetRequest);
        }


        private string ApiPath(string path, object? query)
        {
            string result;
            if (path.FirstOrDefault() == '/')
                result = _apiUrl + path;
            else
                result = _apiUrl + "/" + path;
            if (query != null)
                // var objProperties = query.GetType().GetProperties();
                // var dict = new Dictionary<string, string>();
                // foreach (var prop in objProperties)
                // {
                //     var objVal = prop.GetValue(query);
                //     if (prop.PropertyType.IsArray)
                //     {
                //         dict.Add(prop.Name, objVal);
                //        
                //     }
                //     else
                //     {
                //         var str = objVal?.ToString();
                //         if (str != null) dict.Add(prop.Name, str);
                //     }
                // }

                return $"{result}?{ObjToQueryString(query)}";

            return result;
        }

        private static string ObjToQueryString(object obj)
        {
            var step1 = JsonConvert.SerializeObject(obj);

            var step2 = JsonConvert.DeserializeObject<IDictionary<string, object>>(step1);

            var step3 = step2.SelectMany(x =>
            {
                var type = x.Value.GetType();
                if (type.IsAssignableFrom(typeof(JArray)))
                {
                    var arr = (JArray) x.Value;
                    return arr.Select(e => KeyValue(x.Key, e));
                }

                return new[] {KeyValue(x.Key, x.Value)};
            });

            return string.Join("&", step3);
        }

        private static string KeyValue(string key, object e)
        {
            var value = HttpUtility.UrlEncode(e.ToString());
            return HttpUtility.UrlEncode(key) + "=" + value;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NUnit.Tests")]
namespace WaybackMachineWrapper
{
    public class WaybackClient : IDisposable
    {
        #region static/consts
        private static readonly Uri BASE_URI = new Uri("https://web.archive.org/");
        private const string AVAILABLE_PATH = "wayback/available/?url=";
        private const string SAVE_PATH = "save/";
        #endregion

        #region private
        private HttpClient _client;
        //private IRestRequestFactory _restRequestFactory;
        #endregion

        #region Constructors
        public WaybackClient() : this(new HttpClient(
            new HttpClientHandler() { AllowAutoRedirect=false}
            )) { }

        internal WaybackClient(HttpClient client)
        //public WaybackClient( restClient, IRestRequestFactory restRequestFactory)
        {
            _client = client;
            _client.BaseAddress = BASE_URI;
        }
                
        #endregion
        #region public
        public async Task<AvailableResponse> AvailableAsync(Uri uri )
        {
            //IRestRequest request = _restRequestFactory.Create(AVAILABLE_PATH, Method.GET);
            //request.AddParameter(AVAILABLE_PARAMETER, uri);
            AvailableResponse result = null;
            
            //IRestResponse response = await _restClient.ExecuteGetTaskAsync(request);
            using (HttpResponseMessage response = await _client.GetAsync(AVAILABLE_PATH + uri))
            {
                using (HttpContent content = response.Content)
                {
                    string json = await content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<AvailableResponse>(json);
                    if(result != null) { result.RequestUrl = uri; }
                }
            }
                
            
            return result;
        }

        public AvailableResponse Available(Uri uri)
        {
            Task<AvailableResponse> responseTask = AvailableAsync(uri);
            Task.WaitAll(responseTask);
            return responseTask.Result;
        }

        public async Task<Uri> SaveAsync(Uri uri)
        {
            Uri result = null;
            using (HttpResponseMessage response = await _client.GetAsync(SAVE_PATH + uri))
            {
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Redirect)
                {
                    using (HttpContent content = response.Content)
                    {
                        
                        result = content.Headers.ContentLocation ?? response.Headers.Location;
                        if (!result.IsAbsoluteUri)
                        {
                            result = new Uri(BASE_URI, result.OriginalString);
                        }

                    }
                }
            }
            return result;
        }

        public Uri Save(Uri uri)
        {
            Task<Uri> responseTask = SaveAsync(uri);
            Task.WaitAll(responseTask);
            return responseTask.Result;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
        #endregion
    }
    
    public class AvailableResponse
    {
        public ArchivedSnapshots archived_snapshots {get; set;}
        public Uri RequestUrl { get; set; }

        public AvailableResponse() { }
        public AvailableResponse(Uri requestUrl)
        {
            RequestUrl = requestUrl;
        }

        public class ArchivedSnapshots
        {
            public ArchivedSnapshot closest { get;  set; }
            public class ArchivedSnapshot
            {
                public bool available { get; set; }
                public Uri url { get; set; }
                public string timestamp { get; set; }
                public System.Net.HttpStatusCode status { get; set; }
            }
            
        }
       
    }
}

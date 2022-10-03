using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("NUnit.Tests")]
[assembly: InternalsVisibleTo("NUnit.Tests.Net6")]
namespace WaybackMachineWrapper
{
    public class WaybackClient : IDisposable
    {
        #region static/consts
        private static readonly Regex _captureSpanPattern = new Regex("watchJob\\(\"(.+?)\"");
        private static readonly Uri BASE_URI = new Uri("https://web.archive.org/");
        private const string AVAILABLE_PATH = "wayback/available/?url=";
        private const string SAVE_PATH = "save/";
        private const string SAVE_STATUS_PATH = "save/status/";
        #endregion

        #region private
        private HttpClient _client;
        #endregion

        #region Constructors
        public WaybackClient() : this(new HttpClient(
            new HttpClientHandler() { AllowAutoRedirect=false}
            )) { }

        internal WaybackClient(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = BASE_URI;
            //_client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
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


        /// <summary>
        /// This version of save will post a job request to save a page and poll for the job status.
        /// It relies on a regex to get the job id and is brittle
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="captureErrorPages"></param>
        /// <returns>uri of the saved page resource, otherwise null</returns>
        public async Task<Uri> SaveAsyncV2(Uri uri, bool captureErrorPages = true)
        {
            Uri result = null;

            using (HttpContent requestContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("url", uri.ToString()),
                new KeyValuePair<string,string>("capture_all", "on" )
            }.Take(captureErrorPages ? 2 : 1)))
            using (HttpResponseMessage response = await _client.PostAsync(SAVE_PATH + uri, requestContent))
            {
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Redirect)
                {
                    using (HttpContent content = response.Content)
                    {

                        string htmlContent = await content.ReadAsStringAsync();
                        Match watchJobMatch = _captureSpanPattern.Match(htmlContent);
                        if (watchJobMatch.Success)
                        {
                            string jobId = watchJobMatch.Groups[1].Value;
                            SaveStatusResponse statusResponse = await PollJobStatus(jobId);
                            if (statusResponse != null)
                            {
                                result = new Uri(BASE_URI, $"web/{statusResponse.timestamp}/{uri}");
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// For most sites this save should work, but some require v2.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
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

        private async Task<SaveStatusResponse> PollJobStatus(string jobId)
        {
            string status = null;
            do
            {
                using (HttpResponseMessage response = await _client.GetAsync(SAVE_STATUS_PATH + jobId))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var statusResponse = JsonConvert.DeserializeObject<SaveStatusResponse>(await response.Content.ReadAsStringAsync());
                        if (statusResponse.status == "pending")
                        {
                            await Task.Delay(6000);
                        }
                        else if (statusResponse.status == "success")
                        {
                            return statusResponse;
                        }
                        else
                        {
                            status = statusResponse.status;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            } while (status == null);
            return null;
        }
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
                public HttpStatusCode status { get; set; }
            }
            
        }
    }

    public class SaveStatusResponse
    {
        public bool first_archive { get; set; }
        public int http_status { get; set; }
        public string job_id { get; set; }
        public string status { get; set; }
        public string timestamp { get; set; }
    }
}

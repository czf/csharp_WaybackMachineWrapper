using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaybackMachineWrapper;
using Moq;
using System.Net;
using System.Net.Http;
using System.Globalization;
using System.Threading;
using System.IO;

namespace NUnit.Tests
{
    [TestFixture]
    public class WaybackCLient_Tests
    {
        private const string RESPONSE_AVAILABLE = @"{
    ""archived_snapshots"": {
                        ""closest"": {
                            ""available"": true,
			""url"": ""https://web.archive.org/web/20170312090534/https://www.seattletimes.com/seattle-news/crime/help-me-kill-my-wife-man-accidentally-texts-to-his-former-boss/"",
			""timestamp"": ""20170312090534"",
			""status"": ""200""   
        }
                    }
                }";

        [Test]
        public void TestMockGet()
        {

            HttpMessageHandler mockMessageHandler = new MockMessageHandler(new MockHttpContent(RESPONSE_AVAILABLE));
            WaybackClient client = new WaybackClient(new HttpClient(mockMessageHandler));

            string original = "https://www.something.com";
            AvailableResponse ar = client.Available(new Uri(original));
            
            Assert.IsNotNull(ar);
            Assert.IsNotNull(ar.archived_snapshots);
            Assert.IsNotNull(ar.archived_snapshots.closest);
            Assert.IsNotNull(ar.RequestUrl);
            Assert.IsTrue(ar.archived_snapshots.closest.available);
            Assert.AreEqual(ar.RequestUrl.OriginalString, original);
            Assert.AreEqual(ar.archived_snapshots.closest.status, HttpStatusCode.OK);
        }

        [Test]
        public void TestSeattleTimes()
        {
            WaybackClient client = new WaybackClient();
            AvailableResponse response = client.Available(new Uri("https://www.seattletimes.com"));
        }


        [Test]
        public void TestMockSave()
        {
            MockHttpContent content = new MockHttpContent(string.Empty);
            content.Headers.ContentLocation = new Uri("https://www.expectedoutput.com");
            HttpMessageHandler mockMessageHandler = new MockMessageHandler(content);
            WaybackClient client = new WaybackClient(new HttpClient(mockMessageHandler));


            Uri location = client.Save(new Uri("https://www.uritosave.com"));

            Assert.IsNotNull(location);
            Assert.AreEqual(content.Headers.ContentLocation.AbsoluteUri, location.AbsoluteUri);
        }

        //[Test]
        //public async Task Save()
        //{
        //    WaybackClient client = new WaybackClient();
        //    Uri location = await client.SaveAsyncV2(new Uri("https://www.seattletimes.com/entertainment/movies/new-movies-in-seattle-area-theaters-this-week-knock-at-the-cabin-80-for-brady/"));
        //    Assert.IsNotNull(location);
        //    Assert.IsTrue(location.AbsoluteUri.Contains("archive.org") && location.AbsoluteUri.Contains("seattletimes.com"));
        //}
    }

    internal class MockMessageHandler : HttpMessageHandler
    {
        public HttpContent MockResponseContent;
        public MockMessageHandler(HttpContent content)
        {
            MockResponseContent = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            result.Content = MockResponseContent;
            return Task.FromResult<HttpResponseMessage>(result);
        }
    }

    internal class MockHttpContent : HttpContent
    {
        public string Content { get; set; }

        public MockHttpContent(string content)
        {
            Content = content;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(Content);
            return stream.WriteAsync(byteArray, 0, byteArray.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = Content.Length;
            return true;
        }
    }
}

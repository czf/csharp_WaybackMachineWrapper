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
			""url"": ""http://web.archive.org/web/20170312090534/http://www.seattletimes.com/seattle-news/crime/help-me-kill-my-wife-man-accidentally-texts-to-his-former-boss/"",
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

            string original = "http://www.something.com";
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
            AvailableResponse response = client.Available(new Uri("http://www.seattletimes.com"));
        }


        [Test]
        public void TestMockSave()
        {
            MockHttpContent content = new MockHttpContent(string.Empty);
            content.Headers.ContentLocation = new Uri("http://www.expectedoutput.com");
            HttpMessageHandler mockMessageHandler = new MockMessageHandler(content);
            WaybackClient client = new WaybackClient(new HttpClient(mockMessageHandler));


            Uri location = client.Save(new Uri("http://www.uritosave.com"));

            Assert.IsNotNull(location);
            Assert.AreEqual(content.Headers.ContentLocation.AbsoluteUri, location.AbsoluteUri);
        }

        //[Test]
        //public void Save()
        //{
        //    WaybackClient client = new WaybackClient();
        //    Uri location = client.Save(new Uri("http://www.zappos.com/p/nike-shox-nz-wolf-grey-metallic-gold-anthracite/product/7395033/color/673110"));
        //    Assert.IsNotNull(location);
        //    Assert.IsTrue(location.AbsoluteUri.Contains("archive.org") && location.AbsoluteUri.Contains("zappos.com"));
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

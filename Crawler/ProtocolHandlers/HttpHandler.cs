using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Shaka.Crawl.Store;

namespace Shaka.Crawl.ProtocolHandlers
{
    delegate void OnRecevingStreamHandler(CrawlUrl url, Stream stream);

    class HttpHandler
    {
        #region members

        private HttpWebRequest m_request;
        private HttpWebResponse m_response;

        static string[] s_UserAgents = {"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.63 Safari/537.36"};
        enum UserAgent { Chorme, IE };

        private Encoding m_encoding;

        private CrawlUrl m_CrawlUrl;

        #endregion members

        #region public methods
        public HttpHandler(CrawlUrl crawlUrl) 
            : this()
        {
            this.m_CrawlUrl = crawlUrl;

            this.m_request = (HttpWebRequest)CreateWebRequest(this.m_CrawlUrl);
        }

        public event OnRecevingStreamHandler OnRecevingStream;

        public void RecieveStream()
        {
            if (this.m_request == null)
                throw new ArgumentNullException("m_request");

            this.m_CrawlUrl.StartTime = DateTime.UtcNow;
            try
            {
                //Utilities.DebugLine("------------RequestUrl = {0}", this.m_CrawlUrl.Url);
                this.m_response = (HttpWebResponse)this.m_request.GetResponse();

                if (this.m_response.StatusCode == HttpStatusCode.OK)
                {
                    this.m_CrawlUrl.StatusCode = (int)this.m_response.StatusCode;

                    this.m_CrawlUrl.ContentType = this.m_response.ContentType;
                    this.m_CrawlUrl.ContentLength = this.m_response.ContentLength;
                    this.m_CrawlUrl.CharSet = this.m_response.CharacterSet;
                    this.m_CrawlUrl.LastModified = this.m_response.LastModified;

                    Stream responseStream = this.m_response.GetResponseStream();
                    MemoryStream ms = new MemoryStream();
                    CopyStream(responseStream, ms);

                    if (OnRecevingStream != null)
                        OnRecevingStream(this.m_CrawlUrl, ms);
                }
                else
                {
                    this.m_CrawlUrl.StatusCode = (int)this.m_response.StatusCode;
                }
            }
            catch(System.Net.WebException webEx)
            {
                if (webEx.Response != null)
                {
                    this.m_response = (HttpWebResponse)webEx.Response;
                    this.m_CrawlUrl.StatusCode = (int)this.m_response.StatusCode;
                }
                else
                {
                    this.m_CrawlUrl.StatusCode = -1;
                }

                this.m_CrawlUrl.ErrorID = this.m_CrawlUrl.StatusCode;
                this.m_CrawlUrl.ErrorMessage = webEx.Message;
            }
            catch (Exception ex)
            {
                this.m_CrawlUrl.ErrorID = 600;
                this.m_CrawlUrl.ErrorMessage = ex.Message;
            }
            finally
            {
                this.m_CrawlUrl.EndTime = DateTime.UtcNow;
            }

            Utilities.DebugLine("RequestUrl = {0} - Duration = {1} - CharSet={2} - Status = {3}", this.m_CrawlUrl.Url, 
                (this.m_CrawlUrl.EndTime - this.m_CrawlUrl.StartTime).TotalMilliseconds,
                this.m_CrawlUrl.CharSet,
                this.m_CrawlUrl.StatusCode);

            CrawlStore.GetStore().UpdateCrawlUrl(this.m_CrawlUrl);
        }

        #endregion public methods

        #region private methods
        private HttpHandler()
        {
            this.m_request = null;
            this.m_response = null;
            this.m_encoding = Encoding.UTF8;
            this.m_CrawlUrl = null;
        }

        private static WebRequest CreateWebRequest(CrawlUrl crawlUrl)
        {

            var request = (HttpWebRequest)HttpWebRequest.Create(crawlUrl.Url);

            request.UserAgent = s_UserAgents[(int)UserAgent.Chorme];

            request.UseDefaultCredentials = true;

            return request;
        }

        static void CopyStream(Stream input, Stream output)
        {
            /*
            byte[] buffer = new byte[4096];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
            input.Close();
            output.Position = 0;
            */
            input.CopyTo(output);
            input.Close();
            output.Position = 0;
        }

        #endregion private methods
    }
}

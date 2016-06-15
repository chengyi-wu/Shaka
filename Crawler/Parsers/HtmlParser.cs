using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaka.Crawl.Store;
using System.IO;

namespace Shaka.Crawl.Parsers
{
    internal class HtmlParser : GenericParser
    {
        private const string FallbackEncoding = "UTF-8";

        public HtmlParser(Store.CrawlUrl crawlUrl, System.IO.Stream s)
            : base(crawlUrl, s)
        {

        }
        protected override bool GetChunk()
        {
            Encoding enc = Encoding.GetEncoding(FallbackEncoding);
            if (!string.IsNullOrEmpty(this.m_CrawlUrl.CharSet))
                enc = Encoding.GetEncoding(this.m_CrawlUrl.CharSet);

            HtmlDocument doc = (new HtmlReader(this.m_Stream, enc)).GetDocument();

            int nextBatchId = this.m_CrawlUrl.BatchID + 1;
            var store = CrawlStore.GetStore();

            this.m_CrawlUrl.Title = doc.Title;
            this.m_CrawlUrl.FullText = doc.FullText;

            Utilities.DebugLine("Title={0}", doc.Title);
            Utilities.DebugLine("Anchors={0}", doc.Anchors.Count);
            Utilities.DebugLine("Images={0}", doc.Images.Count);
            //Utilities.DebugLine("************");
            //Utilities.DebugLine(doc.FullText);
            //Utilities.DebugLine("************");

            DateTime dtStart = DateTime.Now;
            foreach (var anchor in doc.Anchors)
            {
                CrawlUrl crawlUrl = null;

                if (!IsLink(anchor))
                {
                    continue;
                }

                if (anchor.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                {
                    crawlUrl = new CrawlUrl(anchor, nextBatchId);
                }
                else
                {
                    string newUrl = string.Format("{0}://{1}{2}{3}", this.m_CrawlUrl.Url.Scheme, this.m_CrawlUrl.Url.Host, 
                        anchor.StartsWith("/") ? string.Empty : "/",
                        anchor);

                    crawlUrl = new CrawlUrl(newUrl, nextBatchId);
                }

                //Utilities.DebugLine("Discovered anchor: {0}", crawlUrl.ToString());

                if(crawlUrl.Host != this.m_CrawlUrl.Host
                    //|| crawlUrl.Url.Segments.Contains("_layouts/")
                    //|| crawlUrl.Url.Segments.Contains("_catalogs/")
                    )
                {
                    continue;
                }

                if (crawlUrl != null)
                {
                    store.Enqueue(crawlUrl);
                }
            }
            TimeSpan duration = DateTime.Now - dtStart;
            Utilities.DebugLine("Enqueue execution time = {0}", duration.TotalMilliseconds);

            return true;
        }

        private static bool IsLink(string url)
        {
            string[] invalidLinks = new string[] { "javascript", "mailto", "#" };

            foreach (var l in invalidLinks)
            {
                if (url.StartsWith(l, StringComparison.InvariantCultureIgnoreCase))
                    return false;
            }

            return true;
        }
    }
}

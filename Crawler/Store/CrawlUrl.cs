using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaka.Crawl.Store
{
    /// <summary>
    /// This calss represents an URL entry from persistent layer.
    /// </summary>
    internal class CrawlUrl : IEquatable<CrawlUrl>
    {
        #region properties

        public string OriginalUrl { get; set; }
        /// <summary>
        /// This is the actual URL.
        /// </summary>
        public Uri Url { get; set; }
        public int ID { get; set; }
        public int StatusCode { get; set; }
        public int ErrorID { get; set; }
        public string ErrorMessage { get; set; }
        public int Hits { get; set; }
        public string CharSet { get; set; }
        public int Weight { get; set; }
        public long ContentLength { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime? LastModified { get; set; }
        public string Title { get; set; }
        public string ContentType { get; set; }
        public int BatchID { get; set; }
        public string FullText { get; set; }

        public string Host
        {
            get
            {
                if (this.Url != null)
                    return this.Url.Host;
                return string.Empty;
            }
        }

        public override int GetHashCode()
        {
            return this.Url.GetHashCode();
        }

        #endregion properties

        internal CrawlUrl()
        {
            this.OriginalUrl = string.Empty;
            this.Url = null;
            this.ID = -1;
            this.StatusCode = 0;
            this.Hits = 0;
            this.CharSet = string.Empty;
            this.Weight = 1;
            this.ContentLength = 0;
            this.ContentType = string.Empty;
            this.StartTime = DateTime.UtcNow;
            this.EndTime = DateTime.UtcNow;
            this.LastModified = null;
            this.BatchID = 0;
            this.ErrorID = 0;
            this.ErrorMessage = string.Empty;
        }

        internal CrawlUrl(string url) :
            this()
        {
            this.OriginalUrl = url;
            this.Url = new Uri(this.OriginalUrl);
        }

        internal CrawlUrl(string url, int batchId)
            : this(url)
        {
            this.BatchID = batchId;
        }

        public override string ToString()
        {
            return this.Url.ToString();
        }

        /// <summary>
        /// Bloom Filter?
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(CrawlUrl other)
        {
            if (other.GetHashCode() == this.GetHashCode())
                return true;
            return false;
        }
    }
}

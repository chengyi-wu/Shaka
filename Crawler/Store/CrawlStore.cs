using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;

namespace Shaka.Crawl.Store
{
    /// <summary>
    /// This is the Queue for URLs
    /// </summary>
    internal abstract class CrawlStore
    {
        protected static CrawlStore s_store;
        protected static object s_sync = new object();
        protected bool m_initialized;

        public static CrawlStore GetStore() { return s_store; }

        /// <summary>
        /// Initialize the queue.
        /// </summary>
        protected CrawlStore()
        {
            this.m_initialized = false;
        }

        /// <summary>
        /// If there was a previous crawl, load it from persistent lyaer.
        /// Otherwise load from StartAddresses.
        /// </summary>
        public virtual void LoadQueues()
        {
            lock (CrawlStore.s_sync)
            {
                if (!this.m_initialized)
                {
                    Utilities.DebugLine("CrawlStore::LoadQueues() - Read from app.config for StartAddresses");
                    string startAddresses = ConfigurationManager.AppSettings["StartAddresses"] ?? string.Empty;

                    if (!string.IsNullOrEmpty(startAddresses))
                    {
                        foreach (var url in startAddresses.Split(new char[] { ';' }))
                        {
                            this.Enqueue(new CrawlUrl(url, 0));
                        }
                    }
                    else
                    {
                        Utilities.DebugLine("CrawlStore::LoadQueues() - StartAddresses is Empty");
                    }

                    this.m_initialized = true;
                }
            }
        }   
        /// <summary>
        /// Add the item to the queue as well as add it to the database
        /// </summary>
        /// <param name="url"></param>
        public abstract void Enqueue(CrawlUrl url);

        /// <summary>
        /// Remove the item from queue and update the status for the item in database.
        /// </summary>
        /// <returns></returns>
        public abstract CrawlUrl Dequeue();

        public abstract void UpdateCrawlUrl(CrawlUrl url);
    }

    internal class InMemoryCrawlStore:CrawlStore
    {
        private Queue<CrawlUrl> m_CrawlingQueue;
        private IDictionary<int, CrawlUrl> m_CrawledQueue;

        public static CrawlStore GetInstance()
        {
            lock (s_sync)
            {
                if (CrawlStore.s_store == null)
                {
                    s_store = new InMemoryCrawlStore();
                }
            }

            return s_store;
        }

        private InMemoryCrawlStore()
        {
            
            this.m_CrawlingQueue = new Queue<CrawlUrl>();
            this.m_CrawledQueue = new Dictionary<int, CrawlUrl>();
        }

        public override void Enqueue(CrawlUrl url)
        {
            if (this.m_CrawledQueue.Keys.Contains(url.GetHashCode()))
            {
                //Get the same item from CrawledQueue and make changes to it.
                var crawledUrl = this.m_CrawledQueue[url.GetHashCode()];
                UpdateCrawlUrl(crawledUrl);
            }
            else
            {
                if (!m_CrawlingQueue.Contains(url))
                {
                    //Utilities.DebugLine("InMemoryCrawlStore::Enqueue() - {0}", url.ToString());
                    this.m_CrawlingQueue.Enqueue(url);
                }
            }
        }

        public override CrawlUrl Dequeue()
        {
            if (this.m_CrawlingQueue.Count == 0)
                return null;

            Utilities.DebugLine(">>CrawledQueue={0}, CrawlingQueue = {1}<<", this.m_CrawledQueue.Count, this.m_CrawlingQueue.Count);

            var url = this.m_CrawlingQueue.Dequeue();

            Utilities.DebugLine("Dequeue URL = {0}", url);

            if (url != null)
                this.m_CrawledQueue.Add(url.GetHashCode(), url);

            return url;
        }

        public override void UpdateCrawlUrl(CrawlUrl url)
        {
            url.Hits++;
            return;
        }
    }
}

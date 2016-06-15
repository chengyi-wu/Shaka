using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Shaka.Crawl.Store;

namespace Shaka.Crawl.Parsers
{
    abstract class GenericParser
    {
        protected Stream m_Stream;
        protected CrawlUrl m_CrawlUrl;

        public GenericParser()
        {
            this.m_Stream = null;
            this.m_CrawlUrl = null;
        }

        public GenericParser(CrawlUrl url, Stream inStream)
        {
            this.m_CrawlUrl = url;
            this.m_Stream = inStream;
        }

        public void GetChunks()
        {
            try
            {
                GetChunk();
            }
            catch (Exception ex)
            {
                this.m_CrawlUrl.ErrorID = 600;
                this.m_CrawlUrl.ErrorMessage = ex.Message;
            }
            finally
            {
                
            }
        }
        protected abstract bool GetChunk(); 
    }
}

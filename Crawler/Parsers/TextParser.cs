using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Shaka.Crawl.Store;

namespace Shaka.Crawl.Parsers
{
    internal class TextParser : GenericParser
    {
        public TextParser(CrawlUrl url, Stream inStream) : base(url, inStream) { }
        protected override bool GetChunk()
        {
            return true;
        }
    }
}

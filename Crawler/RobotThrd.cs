using Shaka.Crawl.Parsers;
using Shaka.Crawl.ProtocolHandlers;
using Shaka.Crawl.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaka.Crawl
{
    class RobotThrd
    {
        #region Members

        #endregion Members

        #region Public Methods
        public RobotThrd()
        {
            this.Init();
        }
        #endregion Public Methods

        #region Private Methods
        private void Init()
        {

        }
        #endregion Private Methods

        public void DoWork(CrawlUrl url)
        {
            if (null == url)
                return;

            HttpHandler handler = new HttpHandler(url);

            handler.OnRecevingStream += (u, s) =>
            {
                GenericParser p = new Shaka.Crawl.Parsers.HtmlParser(u, s);

                p.GetChunks();
            };

            handler.RecieveStream();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Shaka.Crawl.Store;
using System.IO;

namespace Shaka.Crawl
{
    class Crawler
    {
        private CrawlStore Store { get { return  InMemoryCrawlStore.GetInstance(); } }
        static void Main(string[] args)
        {
            Crawler crawler = new Crawler();

            Thread robotThread = new Thread(new ParameterizedThreadStart(crawler.StartRobotThread));

            robotThread.Start();

            robotThread.Join();
        }

        public Crawler()
        {
            Init();
        }

        private void Init()
        {
            this.Store.LoadQueues();
        }

        public void StartRobotThread(object obj)
        {
            var t = new RobotThrd();

            CrawlUrl url = this.Store.Dequeue();
            while (url != null)
            {
                t.DoWork(url);
                url = this.Store.Dequeue();
            }
        }
    }
}

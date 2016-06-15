using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;

namespace Shaka.Crawl
{
    internal static class Utilities
    {
        public static void DebugLine(string format, params object[] args)
        {
            var ln = string.Empty;
            if (args.Length == 0) ln = format;
            else
                ln = string.Format(format, args);
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            ln = string.Format("INFO:\t[ThreadId={1}] {0}", ln, threadId);

            //Debug.WriteLine(ln);
            Trace.WriteLine(ln);
            Console.WriteLine(ln);
        }

        public static string NormalizeUrl(string url)
        {
            if (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1);
            if (url.Contains('%'))
                return HttpUtility.UrlDecode(url.Trim()).ToLower();
            else
                return url.Trim().ToLower();
        }
    }
}

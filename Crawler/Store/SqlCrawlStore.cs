using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;

namespace Shaka.Crawl.Store
{
    internal class SqlCrawlStore:CrawlStore
    {
        private string m_connectionString = string.Empty;

        private enum CrawlState
        {
            Crawled = 0,
            Crawling = 1,
        }

        private SqlCrawlStore()
        {
            this.m_connectionString = ConfigurationManager.AppSettings["CrawlStoreConnection"];
            Utilities.DebugLine("{0}", this.m_connectionString);
        }

        public static CrawlStore GetInstance()
        {
            lock (s_sync)
            {
                if (CrawlStore.s_store == null)
                {
                    s_store = new SqlCrawlStore();
                }
            }

            return s_store;
        }

        public override void Enqueue(CrawlUrl url)
        {
            //Utilities.DebugLine("SqlCrawlStore::Enqueue - {0}", url.ToString());

            ExecuteNonQuery(this.m_connectionString,
                "EnqueueCrawlUrl",
                System.Data.CommandType.StoredProcedure,
                new SqlParameter("@Url", url.Url.ToString()),
                new SqlParameter("@Hash", url.GetHashCode()),
                new SqlParameter("@BatchID", url.BatchID));
        }

        public override CrawlUrl Dequeue()
        {
            CrawlUrl url = null;
            try
            {
                using (SqlConnection conn = new SqlConnection(this.m_connectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandText = "DequeueCrawlUrl";
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Connection = conn;

                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    while(reader.Read())
                    {
                        url = new CrawlUrl(reader["Url"].ToString(),
                             int.Parse(reader["BatchID"].ToString()));

                        url.Hits = int.Parse(reader["Hits"].ToString());

                        Utilities.DebugLine("SqlCrawlStore::Dequeue - {0}", url.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.DebugLine(ex.Message);
            }

            return url;
        }

        public override void UpdateCrawlUrl(CrawlUrl url)
        {
            this.UpdateCrawlUrl(url, CrawlState.Crawled);
        }

        private void UpdateCrawlUrl(CrawlUrl url, CrawlState state)
        {
            ExecuteNonQuery(this.m_connectionString,
                "UpdateCrawlUrl",
                System.Data.CommandType.StoredProcedure,
                new SqlParameter("@Url", url.Url.ToString()),
                new SqlParameter("@Hash", url.GetHashCode()),
                new SqlParameter("@StatusCode", url.StatusCode),
                new SqlParameter("@ErrorID", url.ErrorID),
                new SqlParameter("@ErrorMessage", url.ErrorMessage),
                new SqlParameter("@CharSet", url.CharSet),
                new SqlParameter("@ContentLength", url.ContentLength),
                new SqlParameter("@ContentType", url.ContentType),
                new SqlParameter("@StartTime", url.StartTime),
                new SqlParameter("@EndTime", url.EndTime),
                new SqlParameter("@LastModified", url.LastModified),
                new SqlParameter("@Title", url.Title),
                new SqlParameter("@FullText", url.FullText),
                new SqlParameter("@CrawlState", state)
                );
        }

        private static int ExecuteNonQuery(string connectionString, 
            string cmdTxt,
            System.Data.CommandType cmdType,
            params SqlParameter[] list)
        {
            if (string.IsNullOrEmpty(connectionString)
                || string.IsNullOrEmpty(cmdTxt))
                throw new ArgumentNullException();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandText = cmdTxt;
                    cmd.CommandType = cmdType;

                    if (list != null)
                    {
                        foreach (var p in list)
                        {
                            cmd.Parameters.Add(p);
                        }
                    }
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Utilities.DebugLine(ex.Message);
            }

            return -1;
        }

        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Shaka.Crawl.Parsers
{
    public class HtmlDocument
    {
        public bool WellFormed { get; set; }
        public IList<HtmlElement> Children { get; private set; }
        public HtmlDocument()
        {
            this.Children = new List<HtmlElement>();
            this.WellFormed = true;
        }

        private string m_Title;
        public string Title
        {
            get
            {
                if (m_Title == null)
                {
                    //System.Diagnostics.Debug.Print("HtmlDocument.get_Title");
                    var t = GetElementByTagName(this.Children, "title");
                    if (t == null) return string.Empty;
                    StringBuilder sb = new StringBuilder();
                    ReadInnerText(t, ref sb);
                    m_Title = HttpUtility.HtmlDecode(sb.ToString().Trim());
                }
                return m_Title;
            }
        }

        private IList<string> m_Anchors;
        public IList<string> Anchors
        {
            get
            {
                if (this.m_Anchors == null)
                {
                    //System.Diagnostics.Debug.Print("HtmlDocument.get_Anchors");
                    m_Anchors = new List<string>();
                    ExtractResources("a", "href", ref this.m_Anchors);
                }

                return m_Anchors;
            }
        }

        private IList<string> m_Images;
        public IList<string> Images
        {
            get
            {
                if (m_Images == null)
                {
                    //System.Diagnostics.Debug.Print("HtmlDocument.get_Images");
                    m_Images = new List<string>();
                    ExtractResources("img", "src", ref m_Images);
                }
                return m_Images;
            }
        }

        private static void ReadInnerText(HtmlElement e, ref StringBuilder sb)
        {
            if (e.TagName == "script"
                || e.TagName == "comments"
                || e.TagName == "doctype"
                || e.TagName == "noscript")
                return;
            if (!string.IsNullOrWhiteSpace(e.InnerText))
                sb.Append(e.InnerText.Trim() + Environment.NewLine);

            foreach (var elem in e.Children)
            {
                ReadFullText(elem, ref sb);
            }
        }

        private HtmlElement GetElementByTagName(IList<HtmlElement> elements, string tagName)
        {
            foreach (var e in elements)
            {
                if (e.TagName == tagName)
                    return e;
                if (e.Children.Count > 0)
                {
                    var c = GetElementByTagName(e.Children, tagName);
                    if (c != null) 
                        return c;
                }
            }
            return null;
        }

        private string m_FullText;
        public string FullText
        {
            get
            {
                if (m_FullText == null)
                {
                    //System.Diagnostics.Debug.Print("HtmlDocument.get_FullText");
                    StringBuilder sb = new StringBuilder();
                    foreach (HtmlElement e in this.Children)
                    {
                        ReadFullText(e, ref sb);
                    }
                    m_FullText = HttpUtility.HtmlDecode(sb.ToString().Trim());
                }

                return m_FullText;
            }
        }
        private static void ReadFullText(HtmlElement e, ref StringBuilder sb)
        {
            if (e.TagName == "script"
                || e.TagName == "comments"
                || e.TagName == "doctype"
                || e.TagName == "title"
                || e.TagName == "noscript"
                || e.TagName == "style"
                || e.TagType == HtmlElement.HtmlTagType.EndTag
                || e.NoIndex == true)
            {
                //System.Diagnostics.Debug.Print("Skip {0} for FullText", e.ToString());
                return;
            }
            if (!string.IsNullOrWhiteSpace(e.InnerText))
                sb.Append(e.InnerText.Trim() + Environment.NewLine);

            foreach (var elem in e.Children)
            {
                ReadFullText(elem, ref sb);
            }
        }

        private void ExtractResources(string tagName, string attributeName, ref IList<string> resources)
        {
            foreach (var e in this.Children)
            {
                ExtractResources(e, tagName, attributeName, ref resources);
            }
        }

        private void ExtractResources(HtmlElement e, string tagName, string attributeName, ref IList<string> resources)
        {
            if (e.NoIndex == true)
                return;
            if(e.TagName == tagName 
                && e.TagType != HtmlElement.HtmlTagType.EndTag)
            {
                if(e.Attributes.ContainsKey(attributeName))
                {
                    var val = e.Attributes[attributeName].TrimEnd(new char[] { ';' });
                    if (!string.IsNullOrEmpty(val))
                        resources.Add(HttpUtility.UrlDecode(val));
                }
            }
            else
            {
                foreach (var c in e.Children)
                {
                    ExtractResources(c, tagName, attributeName, ref resources);
                }
            }
        }

    }
}

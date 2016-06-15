using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaka.Crawl.Parsers
{
    public class HtmlElement
    {
        public enum HtmlTagType
        {
            StartTag,
            EndTag,
            InlineTag
        }
        public enum HtmlTagName
        {
            doctype,
            comments,
            meta,
            script,
            input,
            literal,
            footer,
            br
        }
        public IList<HtmlElement> Children { get; private set; }
        public HtmlElement Parent { get; set; }
        public string TagName
        {
            get;
            set;
        }
        public HtmlTagType TagType { get; set; }
        public string InnerText
        {
            get; set;
        }
        public bool NoIndex
        {
            get
            {
                if (this.Attributes.ContainsKey("class"))
                {
                    string val = this.Attributes["class"];

                    foreach(var str in val.Split(new char[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (str == "noindex")
                            return true;
                    }
                    return false;

                    //return val.IndexOf("noindex;", 0, StringComparison.InvariantCultureIgnoreCase) >= 0;
                }
                return false;
            }
        }
        public IDictionary<string, string> Attributes { get; private set; }
        public HtmlElement()
        {
            this.Parent = null;
            this.Children = new List<HtmlElement>();
            this.TagName = string.Empty;
            this.TagType = HtmlTagType.InlineTag;
            this.InnerText = string.Empty;
            this.Attributes = new Dictionary<string, string>();
        }
        public override string ToString()
        {
            //if (this.TagName == HtmlTagName.literal.ToString())
            //    return this.InnerText;
            return string.Format("{0}:{1}", this.TagName, this.TagType);
        }
    }
}

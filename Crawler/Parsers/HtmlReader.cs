#define BUILDDOM

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shaka.Crawl.Parsers
{
    public class HtmlReader
    {
        enum ParserState
        {
            Init = 0,
            EOF,
            OpenTag,
            CloseTag,
            LiteralText,
            CommentsTag,
            ScriptBlock
        }

        private StreamReader m_streamReader;

        private ParserState m_currentState;
#if BUILDDOM
        private HtmlDocument m_document;
#endif
        private HtmlElement m_lastStartElement;
        private HtmlElement m_lastElement;

        private List<HtmlElement> m_elements;

        public List<HtmlElement> Elements { get { return this.m_elements; } }
#if DEBUG
        private string m_rawString;
        public string RawString { get { return this.m_rawString; } }
        public int Position { get { return this.m_rawString.Length - 1; } }
#endif

        public HtmlReader(Stream stream, Encoding encoding)
        {
            using (this.m_streamReader = new StreamReader(stream, encoding))
            {
                this.m_currentState = ParserState.Init;
                this.m_lastStartElement = null;
#if BUILDDOM
                this.m_document = new HtmlDocument();
#endif
                this.m_elements = new List<HtmlElement>();
#if DEBUG
                this.m_rawString = string.Empty;
#endif
                DateTime dtStart = DateTime.Now;

                Thread t = new Thread(new ParameterizedThreadStart(p =>
                {
                    HtmlReader reader = (HtmlReader)p;

                    reader.Parse();
                }));
                t.Name = "ParserWorkerThread";
                
                int parserTimeout = int.Parse(ConfigurationManager.AppSettings["ParserTimeout"]);
                t.Start(this);
                t.Join(parserTimeout * 1000);
                
                if (t.ThreadState != ThreadState.Stopped)
                    throw new Exception(string.Format("Parser uanble to parse the document in {0} seconds", parserTimeout));
            }
        }

        public void Parse()
        {
            while (this.m_currentState != ParserState.EOF)
            {
                char c;
                switch (this.m_currentState)
                {
                    case ParserState.Init:
                        c = ReadNoWhiteSpace();
                        if (c != '<') throw new Exception("Invalid html document");
                        this.m_currentState = ParserState.OpenTag;
                        break;
                    case ParserState.CommentsTag:
                        ParseCommentsTag();
                        break;
                    case ParserState.OpenTag:
                        HtmlElement e = ParseOpenTag();
                        m_lastElement = e;
                        this.m_elements.Add(e);
                        break;
                    case ParserState.CloseTag:
                        this.ParseCloseTag();
                        break;
                    case ParserState.LiteralText:
                        this.ParseLiteralText();
                        break;
                    case ParserState.ScriptBlock:
                        this.ParseScriptBlock();
                        break;
                    case ParserState.EOF:
                    default:
                        return;
                }
            }
        }

        private void ParseScriptBlock()
        {
            HtmlElement e = this.m_lastElement;
            ReadScriptBlock(ref e);
            //e.TagType = HtmlElement.HtmlTagType.InlineTag;

            this.m_lastStartElement = this.m_lastStartElement.Parent;

            HtmlElement scriptEndTag = new HtmlElement();
            scriptEndTag.TagName = HtmlElement.HtmlTagName.script.ToString();
            scriptEndTag.TagType = HtmlElement.HtmlTagType.EndTag;

            this.m_elements.Add(scriptEndTag);
#if BUILDDOM
            if (this.m_lastStartElement == null)
            {
                this.m_document.Children.Add(scriptEndTag);
                this.m_document.WellFormed = false;
            }
            else
            {
                this.m_lastStartElement.Children.Add(scriptEndTag);
            }
            scriptEndTag.Parent = this.m_lastStartElement;
#endif
            if (Peek() == '<')
            {
                Read();
                this.m_currentState = ParserState.OpenTag;
            }
            else
                this.m_currentState = ParserState.LiteralText;
        }

        private void ParseCommentsTag()
        {
            HtmlElement e = this.m_lastElement;
            e.TagName = HtmlElement.HtmlTagName.comments.ToString();
            e.TagType = HtmlElement.HtmlTagType.InlineTag;
            this.m_lastElement = e;

            char c = Read();

            while (!IsEOF())
            {
                if (c == '-')
                {
                    c = Peek();
                    if (c == '-')
                    {
                        Read();
                        if (Peek() == '>')
                        {
                            Read();
                            this.m_currentState = ParserState.CloseTag;
                            return;
                        }
                    }
                }

                e.InnerText += c;
                c = Read();
            }
        }

        private void ParseLiteralText()
        {
            HtmlElement e = this.m_lastStartElement;

            if (e != null 
                && e.TagName == HtmlElement.HtmlTagName.script.ToString() 
                && e.TagType == HtmlElement.HtmlTagType.StartTag)
            {
                this.m_currentState = ParserState.ScriptBlock;
                return;
            }

            HtmlElement txtElment = new HtmlElement();
            txtElment.TagName = HtmlElement.HtmlTagName.literal.ToString();
            txtElment.TagType = HtmlElement.HtmlTagType.InlineTag;
            this.m_lastElement = txtElment;

            if (this.m_lastStartElement != null)
            {
                this.m_lastStartElement.Children.Add(txtElment);

                txtElment.Parent = e;
            }

            char c = Read();

            while (!IsEOF())
            {
                if (c == '<')
                {
                    this.m_currentState = ParserState.OpenTag;
                    return;
                }

                txtElment.InnerText += c;
                c = Read();
            }
        }

        private HtmlElement ParseOpenTag()
        {
            HtmlElement e = new HtmlElement();
            char c = Read();

            e.TagType = HtmlElement.HtmlTagType.StartTag;

            if(c=='/')
            {
                c = Read();
                e.TagType = HtmlElement.HtmlTagType.EndTag;
            }

            if (c == '!')
            {
                c = Peek();

                if (c == '-')
                {
                    Read();
                    if (Peek() == '-')
                    {
                        Read();
                        this.m_currentState = ParserState.CommentsTag;
                        return e;
                    }
                }

                if (IsAlphabetic(c))
                {
                    c = Read();
                    e.TagType = HtmlElement.HtmlTagType.InlineTag;
                }
            }
            
            if(!IsAlphabetic(c))
            {
                throw new Exception("Invalid tag name, it must be alphabetic.");
            }

            while(IsAlphaNumeric(c)
                || c == ':' || c == '_' || c == '-')
            {
                //if (e.TagType == HtmlElement.HtmlTagType.EndTag) { System.Diagnostics.Debug.Print(c.ToString()); }
                e.TagName += c;
                c = Read();
                //if (e.TagName == "aside" && e.TagType == HtmlElement.HtmlTagType.EndTag) { System.Diagnostics.Debug.Print(e.TagName); System.Diagnostics.Debugger.Break(); }
            }
            e.TagName = e.TagName.ToLower();

            //if (e.TagName == "aside" && e.TagType == HtmlElement.HtmlTagType.EndTag) { System.Diagnostics.Debug.Print(e.TagName); System.Diagnostics.Debugger.Break(); }

            if (c == '>')
            {
                this.m_currentState = ParserState.CloseTag;
                return e;
            }

            while (true)
            {
                c = Peek();

                if (c == '>')
                {
                    Read();
                    this.m_currentState = ParserState.CloseTag;

                    if (AllowInlineTag(e.TagName))
                    {
                        e.TagType = HtmlElement.HtmlTagType.InlineTag;
                    }

                    return e;
                }

                if (c == '/')
                {
                    c = Read();
                    e.TagType = HtmlElement.HtmlTagType.InlineTag;
                }

                if (IsAlphabetic(c)
                    //Usually the property name starts with a-Z, but for malformed ones, it might not be the case.
                    //Just allow numbers
                    || IsNumeric(c)
                    || c=='_')
                {
                    ParseAttribute(ref e);
                }

                //When the CloseTag(>) is missing. Try to ignore this malformation.
                if (c == '<')
                {
                    Read();
                    this.m_currentState = ParserState.OpenTag;

                    return e;
                }

                if (IsWhiteSpace(c))
                    c = Read();
            }
        }

        private void ParseAttribute(ref HtmlElement e)
        {
            string attributeName = string.Empty;
            string attributeValue = string.Empty;

            char c = Read();
            while(!IsWhiteSpace(c)
                && c!= '=')
            {
                attributeName += c;
                c = Read();

                if (Peek() == '>')
                {
                    attributeName += c;
                    c = Peek();
                    break;
                }
            }
            attributeName = attributeName.ToLower();
            if(!e.Attributes.ContainsKey(attributeName))
                e.Attributes.Add(attributeName, string.Empty);

            if (c == '>')
                return;

            c = Peek();

            while (true)
            {
                c = Peek();

                if (c == '>'
                    || c == '/')
                {
                    return;
                }

                if (IsAlphabetic(c)
                    || c == '_' || c== ':' || c=='-')
                {
                    ParseAttribute(ref e);
                    return;
                }

                if (c == '\''
                    || c == '"')
                {
                    char stopChar = Read();
                    c = Read();

                    while (c != stopChar)
                    {
                        attributeValue += c;
                        c = Read();
                    }

                    e.Attributes[attributeName] += attributeValue + ";";
                }

                c = Peek();

                if (c == '>'
                    || c == '/')
                {
                    return;
                }

                if (IsAlphabetic(c))
                {
                    ParseAttribute(ref e);
                    return;
                }

                c = Read();
            }
        }

        private void ParseCloseTag()
        {
            HtmlElement e = this.m_lastElement;

            if (e.TagType == HtmlElement.HtmlTagType.StartTag)
            {
#if BUILDDOM
                if (this.m_lastStartElement == null)
                {
                    this.m_document.Children.Add(e);
                }
                else
                {
                    e.Parent = this.m_lastStartElement;
                    this.m_lastStartElement.Children.Add(e);
                }
#endif
                this.m_lastStartElement = e;
            }
            else
            {
                if (e.TagType != HtmlElement.HtmlTagType.InlineTag)
                {
                    if (this.m_lastStartElement != null)
                    {
                        this.m_lastStartElement = this.m_lastStartElement.Parent;
                    }
                    else
                    {
                        //mal-formed
                        //Usually an extra EndTag
                        this.m_document.WellFormed = false;
                    }
                }
#if BUILDDOM
                if (this.m_lastStartElement == null)
                    this.m_document.Children.Add(e);
                else
                {
                    e.Parent = this.m_lastStartElement;
                    this.m_lastStartElement.Children.Add(e);
                }
#endif
            }


            char c = Peek();         

            switch (c)
            {
                case '<':
                    Read();
                    this.m_currentState = ParserState.OpenTag;
                    break;
                default:
                    this.m_currentState = ParserState.LiteralText;
                    break;
            }
        }
        private static bool IsAlphabetic(char c)
        {
            return char.IsLetter(c);
        }

        private static bool IsNumeric(char c)
        {
            return char.IsNumber(c);
        }

        private static bool IsAlphaNumeric(char c)
        {
            return IsAlphabetic(c) || IsNumeric(c);
        }

        private static bool IsWhiteSpace(char c)
        {
            return char.IsWhiteSpace(c);
        }

        private char Read()
        {
            char c = (char)this.m_streamReader.Read();
#if DEBUG
            //if (!this.m_document.WellFormed) System.Diagnostics.Debugger.Break();
            //System.Diagnostics.Debug.Write(c);
            this.m_rawString += c;
#endif
            return c;
        }

        private bool IsEOF()
        {
            if (this.m_streamReader.EndOfStream)
            {
                this.m_currentState = ParserState.EOF;
                this.m_streamReader.Close();
                return true;
            }
            return false;
        }

        private char Peek()
        {
            return (char)this.m_streamReader.Peek();
        }

        private char ReadNoWhiteSpace()
        {
            char c = Read();
            while (IsWhiteSpace(c))
                c = Read();
            return c;
        }

        private void ReadScriptBlock(ref HtmlElement e)
        {
            StringBuilder sb = new StringBuilder();

            char[] stopSequence = "</script".ToCharArray();
            char[] buffer = { '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0'};

            char c = Read() ;
            while (true)
            {
                int i = 0;
                buffer[i] = c;
                while (i < stopSequence.Length && c == stopSequence[i] )
                {
                    i++;
                    c = Read();
                    buffer[i] = c;
                }

                if (i == stopSequence.Length && c=='>')
                {
                    break;
                }
                else
                {
                    for (int j = 0; j < i +1;j++ )
                    {
                        sb.Append(buffer[j]);
                    }
                    c = Read();
                }
                
            }
            
            e.InnerText = sb.ToString();
        }

        private static bool AllowInlineTag(string tagName)
        {
            HtmlElement.HtmlTagName[] tags = { HtmlElement.HtmlTagName.meta, HtmlElement.HtmlTagName.input, HtmlElement.HtmlTagName.footer,  HtmlElement.HtmlTagName.br };

            foreach (var t in tags)
            {
                if (t.ToString() == tagName)
                    return true;
            }
            return false;
        }

#if BUILDDOM
        public HtmlDocument GetDocument()
        {
            return this.m_document;
        }
#endif
    }
}

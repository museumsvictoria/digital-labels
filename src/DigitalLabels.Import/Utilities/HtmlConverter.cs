﻿using System.IO;
using HtmlAgilityPack;

namespace DigitalLabels.Import.Utilities
{
    public static class HtmlConverter
    {
        public static string HtmlToText(string html)
        {
            if (html == null)
                return null;

            var doc = new HtmlDocument();

            doc.LoadHtml(html);

            var sw = new StringWriter();

            ConvertTo(doc.DocumentNode, sw);
            
            return sw.ToString();
        }

        private static void ConvertTo(HtmlNode node, StringWriter outText)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // get text
                    var html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html));
                    }
                    break;

                case HtmlNodeType.Element:
                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }
                    switch (node.Name)
                    {
                        case "p":
                            // treat paragraphs as crlf
                            outText.Write(System.Environment.NewLine + System.Environment.NewLine);
                            break;
                    }

                    break;
            }
        }

        private static void ConvertContentTo(HtmlNode node, StringWriter outText)
        {
            foreach (var subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }
    }
}

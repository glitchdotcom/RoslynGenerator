using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CHtml : CStatement
    {
        private String htmlString;
        private String[] stringArray = null;

        public CHtml(CToken token, String html)
            : base(token)
        {
            htmlString = html;
        }

        public string[] HtmlLines
        {
            get { if (stringArray == null) stringArray = htmlString.Split('\n'); return stringArray; }
            set { stringArray = value; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitHtml(this);
        }

        public String HtmlString
        {
            get { return htmlString; }
            set { htmlString = value; }
        }
    }
}
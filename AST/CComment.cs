using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{

    public class CComment : CStatement
    {
        private String commentString;

        public CComment(CToken token, String com)
            : base(token)
        {
            commentString = com;
        }

        public string Text
        {
            get { return commentString; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitComment(this);
        }
    }
}
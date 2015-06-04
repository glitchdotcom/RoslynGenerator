using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CDim : CStatement, IAttributed
    {
        private List<CVariable> variables = new List<CVariable>();
        private bool inlineComment;

        public CDim(CToken token)
            : base(token)
        {
        }

        public List<CVariable> Variables
        {
            get { return variables; }
        }

        public CAttributeList Attributes
        {
            get { return variables[0].Attributes; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitDim(this);
        }

        public bool InlineComment
        {
            get { return inlineComment; }
            set { inlineComment = value; }
        }
    }
}
using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CReturn : CStatement, INodeParent
    {
        private CExpression expression;

        public CReturn(CToken token)
            : base(token) {}

        public CExpression Expression
        {
            get { return expression; }
            set
            {
                this.expression = value;
                value.Parent = this;
            }
        }

        public void Replace(CNode child, CNode newchild)
        {
            if (child == expression)
                Expression = (CExpression)newchild;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitReturn(this);
        }
    }
}
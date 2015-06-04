using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CParenExpression : CExpression, INodeParent
    {
        private CExpression inner;

        public CParenExpression(CToken tok, CExpression inner)
            : base(tok)
        {
            this.inner = inner;
            inner.Parent = this;
        }

        public CExpression InnerExpression
        {
            get { return inner; }
        }

        public override bool IsConstant
        {
            get
            {
                return inner.IsConstant;
            }
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitParenExpression(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (inner == child)
                inner = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
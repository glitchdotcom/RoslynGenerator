using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CExpressionStatement : CStatement, INodeParent
    {
        private CExpression exp;

        public CExpressionStatement(CExpression exp)
            : base(exp.Token)
        {
            this.exp = exp;
            exp.Parent = this;
        }

        public CExpression InnerExpression
        {
            get { return exp; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitStatement(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == exp)
                exp = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
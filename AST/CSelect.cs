using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    // the class for the switch(select) statement
    public class CSelect : CStatement, INodeParent
    {
        private CExpression pivot; // this is the variable that it swings on
        private CStatementBlock cases; // this will be a vector of case objects

        public CSelect(CToken token, CExpression pivot)
            : base(token)
        {
            cases = new CStatementBlock();
            this.pivot = pivot;
            pivot.Parent = this;
        }

        public CExpression Pivot
        {
            get { return pivot; }
        }

        public CStatementBlock Cases
        {
            get { return cases; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitSelect(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == pivot)
                pivot = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
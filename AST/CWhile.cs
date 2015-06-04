using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CWhile : CStatement, INodeParent
    {
        private CExpression condition;
        private CStatementBlock statements = new CStatementBlock();

        public CWhile(CToken token, CExpression condition)
            : base(token)
        {
            this.condition = condition;
            this.condition.Parent = this;
        }

        public CExpression Condition
        {
            get { return condition; }
        }

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitWhile(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == condition)
                condition = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
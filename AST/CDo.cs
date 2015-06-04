using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CDo : CStatement, INodeParent
    {
        private CExpression condition;
        private CStatementBlock statements = new CStatementBlock();
        private bool bUntil;
        private bool postcondition;

        public CDo(CToken token)
            : base(token)
        {
        }

        public CExpression Condition
        {
            get { return condition; }
            set
            {
                condition = value;
                condition.Parent = this;
            }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitDo(this);
        }

        public CStatementBlock Statements
        {
            get { return statements; }
            set { statements = value; }
        }

        public bool IsDoUntil
        {
            get { return bUntil; }
            set { bUntil = value; }
        }

        public bool IsPostConditionLoop
        {
            get { return postcondition; }
            set { postcondition = value; }
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == condition)
                condition = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
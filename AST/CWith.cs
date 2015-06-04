using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CWith : CStatement, INodeParent
    {
        private CExpression withObj;
        private CStatementBlock statements = new CStatementBlock();

        public CWith(CToken token, CExpression value)
            : base(token)
        {
            withObj = value;
            value.Parent = this;
        }

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public CExpression Value
        {
            get { return withObj; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitWith(this);
        }

        #region INodeParent Members

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (withObj == child)
            {
                withObj = (CExpression)newchild;
                newchild.Parent = this;
            }
        }

        #endregion
    }
}
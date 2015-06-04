using System;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public class CLock : CStatement, INodeParent
    {
        private CExpression lockObj;
        private CStatementBlock statements = new CStatementBlock();

        public CLock(CToken token, CExpression value)
            : base(token)
        {
            lockObj = value;
            value.Parent = this;
        }

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public CExpression Value
        {
            get { return lockObj; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitLock(this);
        }

        #region INodeParent Members

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (lockObj == child)
            {
                lockObj = (CExpression)newchild;
                newchild.Parent = this;
            }
        }

        #endregion
    }
}

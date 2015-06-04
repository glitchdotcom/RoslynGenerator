using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CForEach : CStatement, INodeParent
    {
        private CAccess forVariable;
        private CExpression group;
        private CStatementBlock statements = new CStatementBlock();

        public CForEach(CToken token, CToken var)
            : base(token)
        {
            forVariable = new CAccess(var, var);
        }

        public CExpression Enumerable
        {
            get { return group; }
            set
            {
                group = value;
                group.Parent = this;
            }
        }

        [Obsolete("Use ForVariable instead")]
        public string ForVariableName
        {
            get { return ForVariable.Token.Value; }
        }

        [Obsolete("Use ForVariable instead")]
        public CToken ForVariableToken
        {
            get { return forVariable.Token; }
        }

        public CAccess ForVariable
        {
            get { return forVariable; }
        }

        public CStatementBlock Statements
        {
            get { return statements; }
            set { statements = value; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitForEach(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == group)
                group = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
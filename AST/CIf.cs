using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CIf : CStatement, INodeParent
    {
        private CExpression condition;
        private CStatementBlock thenStatements = new CStatementBlock();
        private CStatementBlock elseStatements = new CStatementBlock();
        private CStatementBlock elseIfBlocks = new CStatementBlock();
        private bool oneLineFlag;
        private bool elseIfFlag;

        public CIf(CToken token, CExpression condition, bool oneLine, bool elseIf)
            : base(token)
        {
            this.condition = condition;
            condition.Parent = this;
            oneLineFlag = oneLine;
            elseIfFlag = elseIf;
        }

        public CExpression Condition
        {
            get { return condition; }
        }

        public CStatementBlock ThenStatements
        {
            get { return thenStatements; }
        }

        public CStatementBlock ElseStatements
        {
            get { return elseStatements; }
        }

        public CStatementBlock ElseIfBlocks
        {
            get { return elseIfBlocks; }
        }

        public bool IsOneLine
        {
            get { return oneLineFlag; }
        }

        public bool IsElseIf
        {
            get { return elseIfFlag; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitIf(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == condition)
                condition = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public abstract class CBinaryOperator : CExpression, INodeParent
    {
        private CToken operation;
        private CExpression lhs;
        private CExpression rhs;

        public CBinaryOperator(CToken tok, CExpression lhs, CExpression rhs)
            : base(tok)
        {
            operation = tok;
            this.lhs = lhs;
            this.rhs = rhs;
            lhs.Parent = this;
            rhs.Parent = this;
        }

        public override bool IsConstant
        {
            get
            {
                return lhs.IsConstant && rhs.IsConstant;
            }
        }

        public CToken Operation
        {
            get { return operation; }
        }

        public CExpression Left
        {
            get { return lhs; }
        }

        public CExpression Right
        {
            get { return rhs; }
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (lhs == child)
                lhs = (CExpression)newchild;
            if (rhs == child)
                rhs = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public abstract class CUnaryOperator : CExpression, INodeParent
    {
        private CToken action;
        private CExpression rhs;

        public CUnaryOperator(CToken tok, CExpression rhs)
            : base(tok)
        {
            action = tok;
            this.rhs = rhs;
            rhs.Parent = this;
        }

        public CToken Operation
        {
            get { return action; }
        }

        public virtual CExpression Operand
        {
            get { return rhs; }
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == rhs)
                rhs = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
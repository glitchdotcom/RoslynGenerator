using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CTernary : CExpression, INodeParent
    {
        private CExpression cond, lhs, rhs;

        public CTernary(CToken tok, CExpression cond, CExpression lhs, CExpression rhs)
            : base(tok)
        {
            this.cond = cond;
            this.lhs = lhs;
            this.rhs = rhs;

            cond.Parent = this;
            lhs.Parent = this;
            rhs.Parent = this;
        }
       
        public override bool IsConstant
        {
            get
            {
                return cond.IsConstant && lhs.IsConstant && rhs.IsConstant;
            }
        }

        public CExpression Cond
        {
            get { return cond; }
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
            if (cond == child)
                cond = (CExpression)newchild;
            if (lhs == child)
                lhs = (CExpression)newchild;
            if (rhs == child)
                rhs = (CExpression)newchild;
            newchild.Parent = this;
        }
        
        public override void Accept(IVisitor visit)
        {
            visit.VisitTernary(this);
        }
    }
}
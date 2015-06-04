using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    // the CSpecialEqual class is for this situation <%=something%> which turns into <% Response.Write something %>
    public class CSpecialEqual : CStatement, INodeParent
    {
        private CExpression expr;

        public CSpecialEqual(CToken token, CExpression value)
            : base(token)
        {
            expr = value;
            expr.Parent = this;
        }

        public CExpression Value
        {
            get { if (expr.Parent != this) throw new DataMisalignedException(); return expr; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitSpecialEqual(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == expr)
                expr = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
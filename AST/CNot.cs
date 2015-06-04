using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CNot : CUnaryOperator
    {
        public CNot(CToken tok, CExpression rhs)
            : base(tok, rhs)
        {
            base.LoadType(BuiltIns.Boolean);
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitNot(this);
        }
    }
}
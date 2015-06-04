using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CComparison : CBinaryOperator
    {
        public CComparison(CToken tok, CExpression lhs, CExpression rhs)
            : base(tok, lhs, rhs)
        {
            base.LoadType(BuiltIns.Boolean);
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitComparison(this);
        }
    }
}
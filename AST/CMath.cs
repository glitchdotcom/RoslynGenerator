using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CMath : CBinaryOperator
    {
        public CMath(CToken tok, CExpression lhs, CExpression rhs)
            : base(tok, lhs, rhs)
        {
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitMath(this);
        }
    }
}
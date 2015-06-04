using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CLogic : CBinaryOperator
    {
        public CLogic(CToken tok, CExpression lhs, CExpression rhs)
            : base(tok, lhs, rhs)
        {
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitLogic(this);
        }
    }
}
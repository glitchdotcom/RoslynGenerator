using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CMathUnary : CUnaryOperator
    {
        public CMathUnary(CToken tok, CExpression rhs)
            : base(tok, rhs)
        {
        }

        public override bool IsConstant
        {
            get
            {
                return Operand.IsConstant;
            }
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitMathUnary(this);
        }
    }
}
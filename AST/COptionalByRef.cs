using System;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public class COptionalByRef : CExpression
    {
        public COptionalByRef(CToken tok)
            : base(tok)
        {
            this.IsPassedByRef = true;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitOptionalByRef(this);
        }
    }
}

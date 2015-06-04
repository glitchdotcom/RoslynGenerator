using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CDirective : CStatement
    {
        public CDirective(CToken token)
            : base(token)
        {
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitDirective(this);
        }
    }
}
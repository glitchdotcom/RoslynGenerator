using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CGlobalAccess : CAccess
    {
        public CGlobalAccess(CToken tok)
            : base(tok, tok)
        {
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitGlobalAccess(this);
        }
    }
}
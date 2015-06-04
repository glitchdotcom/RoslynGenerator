using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CThisAccess : CAccess
    {
        public CThisAccess(CToken tok)
            : base(tok, tok)
        {
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitThisAccess(this);
        }
    }
}
using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CWithAccess : CAccess
    {
        public CWithAccess(CToken tok)
            : base(tok, tok)
        {
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitWithAccess(this);
        }
    }
}
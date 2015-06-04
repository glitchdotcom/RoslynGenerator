using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CBaseAccess : CAccess
    {
        public CBaseAccess(CToken tok)
            : base(tok, tok)
        {
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitBaseAccess(this);
        }
    }
}
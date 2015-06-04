using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CIgnore : CStatement
    {
        public CIgnore(CToken token)
            : base(token)
        {
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitStatement(this);
        }
    }
}
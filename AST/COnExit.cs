using System;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public class COnExit : COnError
    {
        CStatement statement;

        public COnExit(CToken tok, CToken action, CStatement statement)
            : base(tok, action)
        {
            this.statement = statement;
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitOnExit(this);
        }

        public CStatement Statement { get { return statement; } }
    }
}

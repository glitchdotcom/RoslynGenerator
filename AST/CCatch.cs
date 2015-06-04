using System;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public class CCatch : CStatement
    {
        CVariable pivot;
        CStatementBlock statements = new CStatementBlock();

        public CVariable Pivot
        {
            get { return pivot; }
            set { pivot = value; }
        }

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public CCatch(CToken tok)
            : base(tok) { }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitCatch(this);
        }
    }
}

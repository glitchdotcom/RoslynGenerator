using System;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public class CFinally : CStatement
    {
        CStatementBlock statements = new CStatementBlock();

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public CFinally(CToken tok)
            : base(tok)
        {
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitFinally(this);
        }

    }
}

using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class COnError : CStatement
    {
        private CToken action;

        public COnError(CToken tok, CToken action)
            : base(tok)
        {
            this.action = action;
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitOnError(this);
        }

        public CToken Action
        {
            get { return action; }
            set { action = value; }
        }
    }
}
using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CReDim : CDim
    {
        private bool bPreserve;

        public CReDim(CToken token, bool preserve)
            : base(token)
        {
            bPreserve = preserve;
        }

        public bool PreserveArrayContents
        {
            get { return bPreserve; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitReDim(this);
        }
    }
}
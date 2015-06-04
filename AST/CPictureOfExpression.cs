using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CPictureOfExpression : CExpression, INodeParent
    {
        private CAccess access;

        public CPictureOfExpression(CToken tok, CAccess access)
            : base(tok)
        {
            this.access = access;
            this.access.Parent = this;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitPictureOf(this);
        }

        public CAccess AccessTarget
        {
            get { return access; }
            set { access = value; }
        }

        #region INodeParent Members

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == access)
            {
                access = (CAccess)newchild;
                newchild.Parent = this;
            }
        }

        #endregion
    }
}
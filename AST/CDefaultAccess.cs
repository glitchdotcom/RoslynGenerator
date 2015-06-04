using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CDefaultAccess : CAccess, INodeParent
    {
        private CAccess m_targetAccess;
        private CParameters parameters;
        private bool realDefaultMethodCall = false;

        public CDefaultAccess(CToken tok, CAccess item, CParameters parameters)
            : base(tok, tok)
        {
            m_targetAccess = item;
            m_targetAccess.Parent = this;
            this.parameters = parameters;
            this.parameters.Parent = this;
            item.IsCallExplicit = true;
            IsRootAccess = false;
        }

        public CAccess TargetAccess
        {
            get { return m_targetAccess; }
        }

        public CParameters Parameters
        {
            get { return parameters; }
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitDefaultAccess(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == m_targetAccess)
                m_targetAccess = (CAccess)newchild;
            newchild.Parent = this;
            m_targetAccess.IsCallExplicit = true;
        }

        public bool IsRealDefaultMethodCall
        {
            get { return realDefaultMethodCall; }
            set { realDefaultMethodCall = value; }
        }
    }
}
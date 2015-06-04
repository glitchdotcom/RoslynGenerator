using System;
using System.Collections;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public interface INodeParent
    {
        void Replace(CNode child, CNode newchild);
    }

    public enum NodeStateMode
    {
        Initial,
        Server,
        Client,
        MaxStateValue
    }

    public abstract class CNode
    {
        private static List<CNode> allNodes = new List<CNode>();
        private CToken token;
        private CTypeRef type;
        private INodeParent parent;
        private bool _semanticallyComplete = false;
        public event EventHandler TypeChanged;

        protected CNode(CToken token)
        {
            type = new CTypeRef(this);
            this.token = token;
            lock (allNodes) allNodes.Add(this);
        }

        protected CNode()
        {
            type = new CTypeRef(this);
            token = null;
            lock (allNodes) allNodes.Add(this);
        }

        public virtual int LineNumber
        {
            get
            {
                if (token != null)
                    return token.LineNumber;
                return 0;
            }
        }

        public virtual bool SemanticallyComplete
        {
            get { return _semanticallyComplete; }
        }

        public CToken Token
        {
            get { return token; }
        }

        public virtual CTypeRef Type
        {
            get { return type; }
        }

        public virtual void ClearType()
        {
            type = new CTypeRef(this);
        }

        public virtual void LoadType(CClass type)
        {
            this.type.InternalLoad(type);
        }

        public virtual void LoadType(CTypeRef tref)
        {
            type.InternalLoad(tref);
        }

        internal void DoTypeChanged()
        {
            OnTypeChanged();
        }

        protected void OnTypeChanged()
        {
            if (TypeChanged != null) TypeChanged(this, new EventArgs());
        }

        public INodeParent Parent
        {
            get { return parent; }
            protected internal set { parent = value; }
        }

        public virtual void SetSemanticallyComplete()
        {
            if (!type.Resolved)
                throw new InvalidOperationException("Type must be resolved before calling SetSemanticallyComplete()");
            _semanticallyComplete = true;
        }

        public abstract void Accept(IVisitor visitor);

        internal static void ClearSemanticallyCompleteOnAllNodes()
        {
            foreach (CNode node in allNodes)
                node._semanticallyComplete = false;
        }
    }
}

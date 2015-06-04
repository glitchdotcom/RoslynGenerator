using System;
using System.Collections;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CStatementBlock : CNode, IEnumerable<CNode>, INodeParent
    {
        private List<CNode> m_statements = new List<CNode>();
        private bool suppressIndent = false;

        public CStatementBlock()
        {
        }

        public CStatementBlock(CToken tok, bool suppressIndent) : base(tok)
        {
            this.suppressIndent = suppressIndent;
        }

        public void Add(CNode node)
        {
            m_statements.Add(node);
            node.Parent = this;
        }

        public int Count
        {
            get { return m_statements.Count; }
        }

        public int IndexOf(CNode node)
        {
            if (node is CVariable)
                return IndexOf((CVariable)node);
            return m_statements.IndexOf(node);
        }

        private int IndexOf(CVariable var)
        {
            int ix = m_statements.IndexOf(var);
            if (ix >= 0)
                return ix;

            for (ix = 0; ix < m_statements.Count; ix++)
            {
                CNode n = m_statements[ix];
                if (n is CMemberVariable)
                    if (((CMemberVariable)n).Variables.Contains(var))
                        return ix;
            }

            return -1;
        }

        public CNode this[int ix]
        {
            get { return m_statements[ix]; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitBlock(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i] == child)
                    m_statements[i] = newchild;
            }
            newchild.Parent = this;
        }

        public bool SuppressIndent
        {
            get { return suppressIndent; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_statements.GetEnumerator();
        }

        ///<summary>
        ///Returns an enumerator that iterates through the collection.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        IEnumerator<CNode> IEnumerable<CNode>.GetEnumerator()
        {
            return m_statements.GetEnumerator();
        }
    }
}

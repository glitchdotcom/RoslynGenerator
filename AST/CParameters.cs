using System;
using System.Collections;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CParameters : INodeParent
    {
        public class ParameterList : IEnumerable<CNode>
        {
            private readonly INodeParent parent;
            private List<CNode> items = new List<CNode>();

            public ParameterList(INodeParent parent)
            {
                this.parent = parent;
            }

            public ParameterList(INodeParent parent, ParameterList source)
            {
                this.parent = parent;
                items.AddRange(source.items);
            }

            public void Add(CNode parameter)
            {
                if (parameter != null)
                    parameter.Parent = parent;
                items.Add(parameter);
            }

            public void RemoveAt(int ix)
            {
                items.RemoveAt(ix);
            }

            public int Count
            {
                get { return items.Count; }
            }

            public CNode this[int ix]
            {
                get { return items[ix]; }
                set
                {
                    items[ix] = value;
                    value.Parent = parent;
                }
            }

            public void Replace(CNode child, CNode newchild)
            {
                for (int ix = 0; ix < items.Count; ix++)
                {
                    if (items[ix] == child)
                    {
                        items[ix] = newchild;
                        newchild.Parent = parent;
                    }
                }
            }

            internal void UpdateParents()
            {
                foreach (CNode node in items)
                    if (node != null)
                        node.Parent = parent;
            }

            #region IEnumerable<CNode> Members

            IEnumerator<CNode> IEnumerable<CNode>.GetEnumerator()
            {
                return items.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return items.GetEnumerator();
            }

            #endregion
        }

        public class NamedParameterList : object, IEnumerable<KeyValuePair<string, CNode>>, INodeParent
        {
            CParameters parent;
            int count = 0;
            KeyValuePair<string, CNode>[] items;

            public NamedParameterList(CParameters parent, int size)
            {
                this.parent = parent;
                items = new KeyValuePair<string, CNode>[size * 2];
                count = 0;
            }

            public NamedParameterList(CParameters parent)
                : this(parent, 16)
            {
            }

            public NamedParameterList(CParameters parent, NamedParameterList src)
                : this(parent, src.count)
            {
                Array.Copy(src.items, items, src.count);
                count = src.count;
            }

            public CNode this[string key]
            {
                get
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (items[i].Key == key) return items[i].Value;
                    }
                    throw new ArgumentOutOfRangeException("key");
                }
            }

            public KeyValuePair<string, CNode> this[int ix]
            {
                get { return items[ix]; }
            }

            public int Count
            {
                get { return count; }
            }

            #region INodeParent Members

            public void Replace(CNode child, CNode newchild)
            {
                for (int i = 0; i < count; i++)
                {
                    if (items[i].Value == child)
                    {
                        items[i] = new KeyValuePair<string, CNode>(items[i].Key, newchild);
                        newchild.Parent = parent;
                    }
                }
            }

            #endregion

            #region IEnumerable<KeyValuePair<string,CNode>> Members

            public IEnumerator<KeyValuePair<string, CNode>> GetEnumerator()
            {
                for (int i = 0; i < count; i++)
                {
                    yield return items[i];
                }
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                for (int i = 0; i < count; i++)
                {
                    yield return items[i];
                }
            }

            #endregion

            internal bool ContainsKey(string p)
            {
                for (int i = 0; i < count; i++)
                {
                    if (items[i].Key == p) return true;
                }
                return false;
            }

            internal void Add(string p, CExpression cExpression)
            {
                if (items.Length == count)
                    Array.Resize(ref items, items.Length * 2);
                items[count++] = new KeyValuePair<string, CNode>(p, cExpression);
                cExpression.Parent = parent;
            }
        }

        private ParameterList unnamedParams;
        private NamedParameterList namedParams;
        private bool _semanticallyComplete = false;
        private CNode parent;

        public CParameters()
        {
            namedParams = new NamedParameterList(this);
            unnamedParams = new ParameterList(this);
        }

        public CParameters(CParameters @params)
        {
            parent = @params.parent;
            unnamedParams = new ParameterList(this, @params.unnamedParams);
            namedParams = new NamedParameterList(this, @params.namedParams);
        }

        public CParameters(params CExpression[] exprs)
            : this()
        {
            foreach (CExpression node in exprs)
            {
                unnamedParams.Add(node);
            }
        }

        public void Accept(IVisitor visit)
        {
            visit.VisitParameters(this);
        }

        internal List<CNode> GetDistinctNodes()
        {
            List<CNode> list = new List<CNode>();
            foreach (CNode node in unnamedParams)
                if (node != null && !list.Contains(node))
                    list.Add(node);
            foreach (KeyValuePair<string, CNode> pair in namedParams)
                if (pair.Value != null && !list.Contains(pair.Value))
                    list.Add(pair.Value);
            return list;
        }

        public bool SemanticallyComplete
        {
            get { return _semanticallyComplete; }
        }

        public ParameterList Unnamed
        {
            get { return unnamedParams; }
        }

        public NamedParameterList Named
        {
            get { return namedParams; }
        }

        public CNode this[int ix]
        {
            get { return unnamedParams[ix]; }
        }

        public CNode Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public void setSemanticallyComplete()
        {
            _semanticallyComplete = true;
        }

        public void resetSemanticallyComplete()
        {
            _semanticallyComplete = false;
        }

        public void Replace(CNode child, CNode newchild)
        {
            namedParams.Replace(child, newchild);
            unnamedParams.Replace(child, newchild);
        }
    }
}
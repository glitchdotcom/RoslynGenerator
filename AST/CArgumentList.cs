using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public class CArgumentList : IEnumerable<CArgument>
    {
        private Dictionary<string, CArgument> argTable;
        private List<CArgument> args = new List<CArgument>();
        private bool _semanticallyComplete = false;
        private int? minParams;
        private int? maxParams;

        public void Add(CArgument arg)
        {
            minParams = maxParams = null;
            args.Add(arg);
        }

        public CArgument this[int ix]
        {
            get { return args[ix]; }
        }

        public CArgument this[string name]
        {
            get
            {
                BuildArgTable();
                CArgument arg = null;
                argTable.TryGetValue(name, out arg);
                return arg;
            }
        }

        private void BuildArgTable()
        {
            if (argTable != null) return;
            argTable = new Dictionary<string, CArgument>(StringComparer.InvariantCultureIgnoreCase);
            foreach (CArgument arg in args)
            {
                argTable[arg.Name.Value] = arg;
            }
        }

        public CArgumentList GetRange(int index, int count)
        {
            CArgumentList newlist = new CArgumentList();
            newlist.args = args.GetRange(index, count);
            return newlist;
        }

        public int MinAllowedParameters
        {
            get
            {
                if (!minParams.HasValue)
                {
                    GetMinMax();
                }
                return minParams.Value;
            }
        }

        public int MaxAllowedParameters
        {
            get
            {
                if (!maxParams.HasValue)
                {
                    GetMinMax();
                }
                return maxParams.Value;
            }
        }

        private void GetMinMax()
        {
            maxParams = 0;
            minParams = 0;
            foreach (CArgument arg in args)
            {
                bool paramarray = arg.Attributes.contains("paramarray");
                if (!arg.Optional && !paramarray)
                    minParams++;
                if (!paramarray)
                    maxParams++;
                else
                    maxParams = int.MaxValue >> 1;
            }
        }

        public int Count
        {
            get { return args.Count; }
        }

        public bool SemanticallyComplete()
        {
            return _semanticallyComplete;
        }

        public void SetSemanticallyComplete()
        {
            _semanticallyComplete = true;
        }

        public void ResetSemanticallyComplete()
        {
            _semanticallyComplete = false;
        }

        #region IEnumerable<CArgument> Members

        IEnumerator<CArgument> IEnumerable<CArgument>.GetEnumerator()
        {
            return args.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return args.GetEnumerator();
        }

        #endregion

        public IEnumerable<CNode> Nodes()
        {
            foreach (CArgument arg in args)
                yield return arg;
        }

        public string ToArgumentString()
        {
            List<string> sargs = args.ConvertAll<string>(delegate(CArgument arg) { return arg.ToArgumentString(); });
            return String.Join(", ", sargs.ToArray());
        }

        public string ToParameterString()
        {
            List<string> sargs = args.ConvertAll<string>(delegate(CArgument arg) { return arg.ToParameterString(); });
            return String.Join(", ", sargs.ToArray());
        }

        public string ToTypeString()
        {
            List<string> sargs = args.ConvertAll<string>(delegate(CArgument arg) { return arg.ToTypeString(); });
            return String.Join(", ", sargs.ToArray());
        }

        internal bool Contains(string p)
        {
            foreach (CArgument arg in args)
                if (arg.Name.Value == p)
                    return true;
            return false;
        }

        internal int IndexOf(CArgument arg)
        {
            return args.IndexOf(arg);
        }
    }
}
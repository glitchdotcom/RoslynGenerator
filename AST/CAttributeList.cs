using System;
using System.Collections;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CAttributeList : IEnumerable<CAttribute>
    {
        internal List<CAttribute> list = new List<CAttribute>();

        public int Count
        {
            get { return list.Count; }
        }

        public void Add(CToken name, CTypeRef ctr)
        {
            Add(name, null, ctr);
        }

        public void Add(CToken name, CParameters parameters, CTypeRef ctr)
        {
            list.Add(new CAttribute(name, parameters, ctr));
        }

        public void Add(CAttributeList attribs)
        {
            if (attribs != null)
                list.AddRange(attribs.list);
        }

        protected void Add(CAttribute attrib)
        {
            list.Add(attrib);
        }

        private static readonly CAttributeList empty = new CAttributeList();

        public CAttributeList getList(String name)
        {
            if (list.Count == 0)
                return empty;

            CAttributeList result = new CAttributeList();
            foreach (CAttribute attribute in list)
            {
                if (NameEquals(attribute.Name, name))
                    result.Add(attribute);
            }

            return result;
        }

        private bool NameEquals(string p, string name)
        {
            return p.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    (p + "Attribute").Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    p.Equals(name + "Attribute", StringComparison.OrdinalIgnoreCase);
        }

        public CAttributeList getList()
        {
            if (list.Count == 0)
                return empty;

            CAttributeList result = new CAttributeList();
            foreach (CAttribute attribute in list)
                result.Add(attribute);

            return result;
        }

        public bool contains(String name)
        {
            if (list.Count == 0)
                return false;
            name = name.ToLower();
            for (int i = 0; i < list.Count; i++)
            {
                if (NameEquals(this[i].Name, name))
                    return true;
            }
            return false;
        }


        public bool containsMultiple(string name)
        {
            if (list.Count < 2)
                return false;
            name = name.ToLower();

            int found = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (NameEquals(this[i].Name, name))
                    found++;
                if (found > 1)
                    return true;
            }
            return false;
        }

        public bool CanVisit(NodeStateMode mode, bool fAlsoExistsOnClient)
        {
            if (mode == NodeStateMode.Client)
            {
                return contains("executeanywhere")
                    || contains("executeonclient")
                    || contains("executeonserver")
                    || (fAlsoExistsOnClient && contains("alsoexistsonclient"));
            }
            else if (mode == NodeStateMode.Server)
            {
                return contains("executeanywhere")
                    || !contains("executeonclient");
            }
            else
                throw new InvalidOperationException("Unknown mode!");
        }


        public CAttribute this[object nameOrIx]
        {
            get
            {
                if (nameOrIx is String)
                    return this[(String)nameOrIx];
                else
                    return this[Convert.ToInt32(nameOrIx)];
            }
        }

        public CAttribute this[string name]
        {
            get
            {
                if (list.Count == 0)
                    return null;
                for (int i = 0; i < list.Count; i++)
                {
                    if (this[i].Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        return this[i];
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if ((this[i].Name + "Attribute").Equals(name, StringComparison.InvariantCultureIgnoreCase) || (this[i].Name).Equals(name + "Attribute", StringComparison.InvariantCultureIgnoreCase))
                        return this[i];
                }
                return null;
            }
        }

        public CAttribute this[int ix]
        {
            get { return list[ix]; }
        }

        ///<summary>
        ///Returns an enumerator that iterates through the collection.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        IEnumerator<CAttribute> IEnumerable<CAttribute>.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        ///<summary>
        ///Returns an enumerator that iterates through a collection.
        ///</summary>
        ///
        ///<returns>
        ///An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CMemberDictionary : CNode, IEnumerable<KeyValuePair<string, CMember>>
    {
        private List<KeyValuePair<string, CMember>> members = new List<KeyValuePair<string, CMember>>();
        private Dictionary<string, CMember> memberDict = new Dictionary<string, CMember>();

        public int Count
        {
            get { return members.Count; }
        }

        public IEnumerable<CMember> Values
        {
            get
            {
                // We iterate over our list of members to preserve order.
                foreach (KeyValuePair<string, CMember> pair in members)
                {
                    yield return pair.Value;
                }
            }
        }

        public override void Accept(IVisitor visitor)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string name)
        {
            return memberDict.ContainsKey(name);
        }

        public CMember this[string name]
        {
            get
            {
                return memberDict[name];
            }
            set
            {
                if (memberDict.ContainsKey(name))
                {
                    for (int ix = 0; ix < members.Count; ix++)
                    {
                        if (String.CompareOrdinal(members[ix].Key, name) == 0)
                        {
                            members[ix] = new KeyValuePair<string, CMember>(name, value);
                            memberDict[name] = value;
                            return;
                        }
                    }
                }
                else
                {
                    members.Add(new KeyValuePair<string, CMember>(name, value));
                    memberDict[name] = value;
                }
            }
        }

        ///<summary>
        ///Returns an enumerator that iterates through the collection.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        IEnumerator<KeyValuePair<string, CMember>> IEnumerable<KeyValuePair<string, CMember>>.GetEnumerator()
        {
            return members.GetEnumerator();
        }

        ///<summary>
        ///Returns an enumerator that iterates through a collection.
        ///</summary>
        ///
        ///<returns>
        ///An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>2</filterpriority>
        public IEnumerator GetEnumerator()
        {
            return members.GetEnumerator();
        }

        public bool TryGetValue(string name, out CMember result)
        {
            return memberDict.TryGetValue(name, out result);
        }

        public void Clear()
        {
            members.Clear();
            memberDict.Clear();
        }
    }

    public abstract class CMember : CNode, IHasVisibility
    {
        private readonly String name;
        private readonly String memberType;
        private readonly CNode[] declared;
        private readonly bool isUnionMember;

        public CMember(CToken tok, string name, string memberType, int declaredSize, bool isUnionMember)
            : base(tok)
        {
            this.name = name;
            this.memberType = memberType;
            declared = new CNode[declaredSize];
            this.isUnionMember = isUnionMember;
        }

        public abstract TokenTypes Visibility { get; }
        public abstract CClass DeclaringClass { get; }

        public string Name
        {
            get { return name; }
        }

        private string rawname = null;
        public string RawName
        {
            get
            {
                if (rawname != null)
                    return rawname;

                switch (memberType)
                {
                    case "method":
                        rawname = ((CMethod)this).Function.RawName;
                        break;
                    case "property":
                        rawname = ((CProperty)this).GetAccessor.RawName;
                        break;
                    case "field":
                        rawname = ((CField)this).Variable.Name.RawValue;
                        break;
                    case "const":
                        rawname = ((CClassConst)this).Constant.Name.RawValue;
                        break;
                    case "override":
                        CMemberOverload cmo = (CMemberOverload)this;
                        foreach (var x in cmo.Overloads)
                        {
                            if (rawname == null)
                                rawname = x.RawName;
                            else if (rawname != x.RawName)
                                throw new NotImplementedException(memberType + " with " + rawname + " and " + x.RawName);
                        }
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                return rawname;
            }
        }

        public string MemberType
        {
            get { return memberType; }
        }

        public override void ClearType()
        {
            if (declared[0] != null)
                declared[0].ClearType();
        }

        public override void LoadType(CClass type)
        {
            if (declared[0] != null)
                declared[0].LoadType(type);
        }

        public override void LoadType(CTypeRef tref)
        {
            if (declared[0] != null)
                declared[0].LoadType(tref);
        }

        public override CTypeRef Type
        {
            get
            {
                if (declared[0] == null)
                    return new CTypeRef(this, (CClass)null);
                return declared[0].Type;
            }
        }

        public CNode[] Declared
        {
            get { return declared; }
        }

        public bool IsUnionMember { get { return isUnionMember; } }

        public override void Accept(IVisitor visitor)
        {
            foreach (CNode node in declared)
            {
                if (node != null)
                    node.Accept(visitor);
            }
        }

        public override bool SemanticallyComplete
        {
            get
            {
                for (int i = 0; i < declared.Length; i++)
                {
                    if (declared[i] != null && !declared[i].SemanticallyComplete)
                        return false;
                }
                return true;
            }
        }

        public override void SetSemanticallyComplete()
        {
            throw new InvalidOperationException("Class members are dynamically semantically complete");
        }

        bool hasExplicit = false;
        string strExplicit = "Error In Compiler";
        internal bool HasExplicitInterface { get { return hasExplicit; } }
        internal string ExplicitInterfaceName
        {
            get
            {
                if (hasExplicit)
                    return strExplicit;
                throw new InvalidOperationException(this.name + " does not have an explicit interface.");
            }
        }

        internal void SetExplicitInterface(string rawIfaceName)
        {
            hasExplicit = true;
            strExplicit = rawIfaceName;
        }

        bool isStatic = false;
        public bool IsStatic
        {
            get { return isStatic; }
            set { isStatic = value; }
        }

        public string VisibilityString
        {
            get
            {
                switch (Visibility)
                {
                    default:
                    case TokenTypes.visPrivate:
                        return "Private";
                    case TokenTypes.visProtected:
                        return "Protected";
                    case TokenTypes.visPublic:
                        return "Public";
                    case TokenTypes.visInternal:
                        return "Internal";
                }
            }
        }

        public virtual IEnumerable<CMember> Overloads
        {
            get
            {
                return null;
            }
        }
    }
}
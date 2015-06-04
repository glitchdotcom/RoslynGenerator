using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;

namespace FogCreek.Wasabi.AST
{
    [DebuggerDisplay("Name: {RawName}")]
    public class CClass : CStatement, IEquatable<CClass>, IAttributed, IHasVisibility
    {
        private CScope clientscope = new CScope();
        private CScope serverscope = new CScope();

        public virtual CScope Scope
        {
            get
            {
                if (Compiler.Current.CurrentMode == NodeStateMode.Client)
                    return clientscope;
                else
                    return serverscope;
            }
        }

        private CAttributeList attribs = new CAttributeList();
        private CToken className;
        private readonly String originalClassName;
        private bool canConvertToString;
        private CStatementBlock statements = new CStatementBlock();
        private CMemberDictionary members = new CMemberDictionary();
        private CMemberDictionary classMembers = new CMemberDictionary();
        private CMember serverdefaultMember = null;
        private CMember clientdefaultMember = null;
        private Hashtable convertsTo = new Hashtable();
        private bool isNumeric = false;
        private bool isObject = true;
        private bool isInferable = true;
        private List<CMember> duplicateMembers = new List<CMember>();
        private List<CTypeRef> interfaces = new List<CTypeRef>();
        private CTypeRef baseClass;
        private bool isAbstract;
        private bool isPublic = true;
        private bool isSealed;
        private TypeDefinition cecilType = null;

        public CClass(CToken token, String rawname, String name, CTypeRef baseClass)
            : base(token)
        {
            originalClassName = name;
            className = CToken.Identifer(token, name, rawname);
            canConvertToString = false;

            this.baseClass = new CTypeRef(this, baseClass);
            base.LoadType(this);
        }

        public CClass(String name, bool canConvertToString)
            : this(new CToken(), name, name, new CTypeRef(null, BuiltIns.Variant))
        {
            this.canConvertToString = canConvertToString;
        }

        /// <summary>
        /// Only used for creating Variant
        /// </summary>
        /// <param name="name">The name of the Variant type</param>
        /// <param name="parent">Set it to null!</param>
        internal CClass(String name, CClass parent)
            : this(new CToken(), name, name, new CTypeRef(null, parent))
        {
        }

        public CClass(CToken token, String name)
            : this(token, name, name, new CTypeRef(null, BuiltIns.Variant))
        {
        }

        public override void ClearType()
        {
            throw new InvalidOperationException();
        }

        public override void LoadType(CClass type)
        {
            throw new InvalidOperationException();
        }

        public override void LoadType(CTypeRef tref)
        {
            throw new InvalidOperationException();
        }

        public TypeDefinition CecilType
        {
            get { return cecilType; }
            set { cecilType = value; }
        }

        public void EnableConversionTo(CClass value)
        {
            if (value == BuiltIns.String)
                canConvertToString = true;
            else
                convertsTo[value] = null;
        }

        public CMember Constructor
        {
            get
            {
                CMember result;
                if (members.TryGetValue(className.Value, out result))
                    return result;
                return null;
            }
        }

        public virtual CMember LookupMember(string k)
        {
            CMember direct = GetDirectMember(k);
            if (direct != null)
                return direct;

            if (BaseClass.Resolved)
                return BaseClass.ActualType.LookupMember(k);

            return null;
        }

        internal CMember LookupClassMember(string member)
        {
            CMember locl;

            if (!classMembers.TryGetValue(member, out locl))
                if (BaseClass.Resolved)
                    return BaseClass.ActualType.LookupClassMember(member);

            return locl;
        }

        public virtual CMember GetDirectMember(string name)
        {
            CMember result;
            members.TryGetValue(name, out result);
            return result;
        }

        internal CMember GetClassDirectMember(string name)
        {
            CMember result;
            classMembers.TryGetValue(name, out result);
            return result;
        }

        public virtual void ClearMembers()
        {
            members.Clear();
        }

        public virtual void SetMember(string k, CMember v)
        {
            members[k] = v;
        }

        public virtual void SetClassMember(string k, CMember v)
        {
            classMembers[k] = v;
        }

        public virtual void AddConstant(string k, CConst v)
        {
            classMembers[k] = new CClassConst(this, v);
        }

        public virtual IEnumerable<CMember> DirectMemberIterator
        {
            get { return members.Values; }
        }

        public virtual IEnumerable<CMember> DirectClassMemberIterator
        {
            get { return classMembers.Values; }
        }

        public virtual IEnumerable<CMember> InheritedMemberIterator
        {
            get
            {
                foreach (CMember member in DirectMemberIterator)
                    yield return member;

                if (BaseClass.Resolved)
                    foreach (CMember member in BaseClass.ActualType.InheritedMemberIterator)
                        yield return member;
            }
        }

        public virtual CMember DefaultMember
        {
            get
            {
                CMember baseDefault = null;
                if (BaseClass.Resolved)
                {
                    baseDefault = baseClass.ActualType.DefaultMember;
                }
                if (Compiler.Current.CurrentMode == NodeStateMode.Client)
                    return clientdefaultMember ?? baseDefault;
                else
                    return serverdefaultMember ?? baseDefault;
            }
            set
            {
                bool executeAnywhere = Attributes.contains("ExecuteAnywhere") || Attributes.contains("AlsoExistsOnClient");
                if (executeAnywhere || Compiler.Current.CurrentMode == NodeStateMode.Client)
                    clientdefaultMember = value;
                if (executeAnywhere || Compiler.Current.CurrentMode != NodeStateMode.Client)
                    serverdefaultMember = value;
            }
        }

        public virtual CTypeRef BaseClass
        {
            get { return baseClass; }
        }

        internal virtual void AddInterface(CTypeRef interfaceref)
        {
            this.interfaces.Add(new CTypeRef(this, interfaceref));
        }

        public IEnumerable<CTypeRef> Interfaces
        {
            get
            {
                return interfaces;
            }
        }

        internal void ResolveBaseClass(CClass klass)
        {
            if (!baseClass.Resolved)
            {
                ForceSetBaseClass(klass);
            }
        }

        protected internal void ForceSetBaseClass(CClass klass)
        {
            baseClass.InternalLoad(klass);
        }

        public bool IsAbstract
        {
            get { return isAbstract; }
            set { isAbstract = value; }
        }

        public bool IsSealed
        {
            get { return isSealed; }
            set { isSealed = value; }
        }

        public bool IsPublic
        {
            get { return isPublic; }
            set { isPublic = value; }
        }

        public virtual bool IsInterface
        {
            get { return this is CInterface; }
        }

        public virtual bool IsEnum
        {
            get { return this is CEnum; }
        }

        public virtual CAttributeList Attributes
        {
            get { return attribs; }
        }

        public string Name
        {
            get { return className.Value; }
        }

        public string RawName
        {
            get { return className.RawValue; }
        }

        string rawNameSpace;
        public string RawNameSpace
        {
            get
            {
                return rawNameSpace ??
                    (rawNameSpace = RawName.Contains(".") ? RawName.Substring(0, RawName.LastIndexOf(".")) : "");
            }
        }

        string nameSpace;
        public string NameSpace
        {
            get
            {
                return nameSpace ??
                    (nameSpace = Name.Contains(".") ? Name.Substring(0, Name.LastIndexOf(".")) : "");
            }
        }

        string rawShortName;
        public string RawShortName
        {
            get
            {
                return rawShortName ??
                    (rawShortName = RawName.Contains(".") ? RawName.Substring(RawName.LastIndexOf(".") + 1) : RawName);
            }
        }

        string shortName;
        public string ShortName
        {
            get
            {
                return shortName ??
                    (shortName = Name.Contains(".") ? Name.Substring(Name.LastIndexOf(".") + 1) : Name);
            }
        }

        public virtual CToken NameToken
        {
            get { return className; }
            protected set { className = value; }
        }

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public bool IsNumeric
        {
            get { return isNumeric; }
            set { isNumeric = value; }
        }

        public bool IsObject
        {
            get { return isObject; }
            set { isObject = value; }
        }

        public bool IsInferable
        {
            get { return isInferable; }
            set { isInferable = value; }
        }

        public List<CMember> DuplicateMembers
        {
            get { return duplicateMembers; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitClass(this);
        }

        protected virtual bool canUnionConvert(CClass klass)
        {
            return false;
        }

        protected virtual bool canArrayConvert(CArrayType klass)
        {
            return false;
        }

        public virtual List<CVariable> GetFields()
        {
            List<CVariable> vars = new List<CVariable>();
            foreach (KeyValuePair<string, CMember> pair in members)
            {
                if (pair.Value.MemberType == "field")
                    vars.Add((CVariable)pair.Value.Declared[0]);
            }
            return vars;
        }

        public virtual List<CMember> GetDirectMembers()
        {
            return new List<CMember>(this.DirectMemberIterator);
        }

        public virtual bool canConvertTo(CClass klass)
        {
            // check the easy cases
            if (ReferenceEquals(klass, this))
                return true;
            else if (ReferenceEquals(klass, null))
                return false;

                // convert union types
            else if (this is CUnionType)
                return canUnionConvert(klass);
            else if (klass is CUnionType)
            {
                CUnionType union = ((CUnionType)klass);
                foreach (CClass type in union.Types)
                {
                    if (canConvertTo(type))
                        return true;
                }
                return false;
            }

                // check for nothing conversions
            else if (klass.className.Value == ("Nothing"))
                return isObject || (this is CArrayType) || this == BuiltIns.String;
            else if (className.Value == ("Nothing"))
                return klass.isObject || (klass is CArrayType) || klass == BuiltIns.String;

                // Variants can convert to anything but Null
            else if ((klass.className.Value == ("__Variant") || className.Value == ("__Variant")) &&
                     !(klass.className.Value == ("DbNull") || className.Value == ("DbNull")))
                return true;

                // check for strings
            else if (klass.className.Value == ("String"))
                return canConvertToString;

                // Object is like variant but only for objects
            else if ((klass.className.Value == ("__Object") || className.Value == ("__Object")) &&
                     (klass.isObject && isObject))
                return true;

                // Check for Array casts
            else if (this is CArrayType && klass is CArrayType)
                return canArrayConvert((CArrayType)klass);

                // Casts are always possible to base classes.
            else if (CClass.IsSubClass(this, klass))
                return true;
            // Casts are always possible to interfaces.
            else if (CClass.Implements(this, klass))
                return true;

                // Otherwise use the convertsTo hash
            else
            {
                if (klass.className.Value == className.Value)
                    throw new ApplicationException(className.RawValue + ": Multiple class objects with the same name");

                return convertsTo.ContainsKey(klass);
            }
        }

        public override bool Equals(Object obj)
        {
            return Equals((CClass)obj);
        }

        public static bool operator ==(CClass lhs, CClass rhs)
        {
            if (ReferenceEquals(lhs, null))
                return ReferenceEquals(rhs, null);
            return lhs.Equals(rhs);
        }

        public static bool operator !=(CClass lhs, CClass rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return originalClassName.GetHashCode();
        }

        /// <summary>
        /// Are classes identical or do they have the same name?
        /// </summary>
        public bool Equals(CClass other)
        {
            if (ReferenceEquals(other, this))
                return true;

            if (ReferenceEquals(other, null))
                return false;

            return string.Equals(other.className.Value, className.Value, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool CanAccess(CClass current, IHasVisibility member)
        {
            return member.Visibility == TokenTypes.visPublic || member.Visibility == TokenTypes.visInternal ||
                (current != null && member.Visibility == TokenTypes.visProtected && IsSubClass(current, member.DeclaringClass)) ||
                current == member.DeclaringClass;
        }

        internal static bool IsSubClass(CClass current, CClass parent)
        {
            if (current == null)
                return false;
            if (current == parent)
                return true;
            else if (current == BuiltIns.Variant)
                return false;
            else
                return IsSubClass(current.BaseClass.ActualType, parent);
        }

        internal static bool Implements(CClass current, CClass intrface)
        {
            if (current == null || intrface == null)
                return false;
            if (current == intrface)
                return true;
            foreach (CTypeRef inter in current.Interfaces)
            {
                if (Implements(inter.ActualType, intrface))
                    return true;
            }
            return Implements(current.BaseClass, intrface);
        }


        #region IHasVisibility Members

        public CClass DeclaringClass
        {
            get { return null; }
        }

        public TokenTypes Visibility
        {
            get
            {
                if (IsPublic)
                    return TokenTypes.visPublic;
                else
                    return TokenTypes.visPrivate;
            }
        }

        #endregion

        #region silly Explicit Interface Overloading stuff
        Dictionary<string, Dictionary<string, CMember>> explicitInterfaces = new Dictionary<string, Dictionary<string, CMember>>();

        public CMember GetExplicitInterface(string name, string interfaceName)
        {
            Dictionary<string, CMember> typeDict;
            if (explicitInterfaces.TryGetValue(interfaceName, out typeDict))
            {
                CMember val;
                typeDict.TryGetValue(name, out val);
                return val;
            }

            return null;
        }

        internal void SetExplicitInterfaceMember(string name, CMember member, string ifaceName)
        {
            Dictionary<string, CMember> typeDict;

            if (!explicitInterfaces.TryGetValue(ifaceName, out typeDict))
            {
                typeDict = new Dictionary<string, CMember>();
                explicitInterfaces[ifaceName] = typeDict;
            }
            typeDict[name] = member;
        }

        internal IEnumerable<CMember> ExplicitInterfaceIterator
        {
            get
            {
                foreach (Dictionary<string, CMember> dict in explicitInterfaces.Values)
                    foreach (CMember memb in dict.Values)
                        yield return memb;
            }
        }
        #endregion

        internal void SetInterface(int i, CTypeRef itr)
        {
            interfaces[i] = itr;
        }

        public bool IsStatic
        {
            get { return false; }
        }

        internal bool IsAConstructor(CFunction function)
        {
            if (function == null)
                return false;
            CMember ctor = this.Constructor;
            if (null == ctor)
                return false;
            switch (ctor.MemberType)
            {
                case "method":
                    return ((CMethod)ctor).Function == function;
                case "override":
                    CMemberOverload cmo = (CMemberOverload)ctor;
                    foreach (CMember ctor_overload in cmo.Overloads)
                    {
                        if (((CMethod)ctor_overload).Function == function)
                            return true;
                    }

                    return false;
                default:
                    throw new InvalidOperationException(ctor.MemberType);
            }

        }
    }
}

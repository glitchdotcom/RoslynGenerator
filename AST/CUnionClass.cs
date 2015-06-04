using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CUnionType : CClass
    {
        private List<CTypeRef> types = new List<CTypeRef>();
        private static int unnamedCount;

        public CUnionType(CToken token, String rawname, String name)
            : base(token, "__Unamed_Union" + unnamedCount++)
        {
            CProgram.Global.AddClass(base.Name, this);
            NameToken = CToken.Identifer(token, name, rawname);
            CProgram.Global.AddClass(base.Name, this);
        }

        public CUnionType(CToken token)
            : base(token, "__Unamed_Union" + unnamedCount++)
        {
            CProgram.Global.AddClass(base.Name, this);
            NameToken = CToken.Identifer(token, "", "");
            IsObject = false;
        }

        [Obsolete("Use Add(CTypeRef tref) instead")]
        public virtual void Add(Object type)
        {
            Add(type, type as string);
        }

        [Obsolete("Use Add(CTypeRef tref) instead")]
        public virtual void Add(Object type, String rawTypeName)
        {
            if (type is CTypeRef)
                Add((CTypeRef)type);
            else if (type is CUnionType)
                Add((CUnionType)type);
            else if (type is CClass)
                Add((CClass)type);
            else
                Add((string)type, rawTypeName);
        }

        [Obsolete("Use Add(CTypeRef tref) instead")]
        public virtual void Add(CClass type)
        {
            Add(new CTypeRef(this, type));
        }

        [Obsolete("Use Add(CTypeRef tref) instead")]
        public virtual void Add(String type, String rawtype)
        {
            Add(new CTypeRef(this, CToken.Identifer(null, type, rawtype)));
        }

        public virtual void Add(CTypeRef tref)
        {
            if (tref.Resolved && tref.ActualType is CUnionType)
                Add((CUnionType)tref.ActualType);
            else
            {
                string prefix = " Or ";
                if (Name == "")
                    prefix = "";
                NameToken =
                    CToken.Identifer(NameToken, Name + prefix + tref.TypeName.Value,
                                     RawName + prefix + tref.TypeName.RawValue);
                types.Add(new CTypeRef(this, tref));
            }
        }

        public virtual void Add(CUnionType union)
        {
            foreach (CTypeRef type in union.types)
                Add(type);
        }

        public override CAttributeList Attributes
        {
            get
            {
                CAttributeList cal = new CAttributeList();
                cal.Add(base.Attributes);

                if (!types[0].Resolved)
                    return cal;

                foreach (CAttribute at in types[0].ActualType.Attributes)
                {
                    bool found = true;
                    IEnumerator<CTypeRef> it = types.GetEnumerator();
                    it.MoveNext();
                    while (found && it.MoveNext())
                    {
                        if (!it.Current.Resolved)
                            found = false;
                        else if (!it.Current.ActualType.Attributes.contains(at.Name))
                            found = false;
                    }

                    if (found)
                        cal.Add(at.NameToken, at.Parameters, new CTypeRef(at, at.Type));
                }

                return cal;
            }
        }

        public virtual String UpdateMembers(IVisitor checker)
        {
            if (types.Count == 0)
                return "Empty enum type used";

            bool isObject = true;
            for (int i = 0; i < types.Count; i++)
            {
                CTypeRef otype = types[i];
                if (!otype.Resolved)
                {
                    CClass ntype = CProgram.Global.FindClass(otype.TypeName);
                    if (ntype == null)
                        return "Cannot find type " + otype.TypeName.RawValue;
                    otype.InternalLoad(ntype);
                }
                types[i] = otype;
                types[i].ActualType.Accept(checker);
                isObject = isObject && otype.ActualType.IsObject;
            }
            IsObject = isObject;

            base.ClearMembers();
            Scope.Clear();

            foreach (CMember member in types[0].ActualType.InheritedMemberIterator)
            {
                bool found;
                if (member is CMemberOverload)
                    found = false;
                else// Unions can only access public members
                    found = member.Visibility == TokenTypes.visPublic;


                IEnumerator<CTypeRef> it = types.GetEnumerator();
                it.MoveNext();
                while (found && it.MoveNext())
                {
                    CClass type = it.Current.ActualType;
                    CMember luMember = type.LookupMember(member.Name);
                    if (luMember == null)
                        found = false;
                    else if (luMember.MemberType != member.MemberType)
                        // one's a method, the other's a field, or etc...
                        found = false;
                    else if (luMember.Visibility != TokenTypes.visPublic)
                        found = false;
                    else
                    {
                        switch (luMember.MemberType)
                        {
                            case "method":
                                CMethod metho = (CMethod)member;
                                CMethod luMetho = (CMethod)luMember;
                                // already checked return type, let's try the parameters
                                if (metho.Function.Arguments.Count != luMetho.Function.Arguments.Count)
                                    found = false;
                                break;
                            case "property":
                                found = UnionProperty((CProperty)member, (CProperty)luMember);
                                // dur
                                break;
                            case "field":
                                // already checked return type, nothing left to check
                                break;
                            case "override":
                                found = false;// dur
                                break;
                        }
                    }
                }
                if (found)
                {
                    CMember fmember;
                    switch (member.MemberType)
                    {
                        case "method":
                            fmember = new CMethod((CMethod)member, true);
                            break;
                        case "property":
                            fmember = new CProperty((CProperty)member, true);
                            break;
                        case "field":
                            fmember = new CField((CField)member, true);
                            break;
                        case "override":
                            fmember = new CMemberOverload((CMemberOverload)member, true);
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                    SetMember(member.Name, fmember);
                    Scope.add(fmember);
                }
            }

            bool hasDefault = true;
            DefaultMember = null;
            foreach (CTypeRef _class in types)
            {
                var memberBase = ((CClass)types[0]).DefaultMember;
                var member = _class.ActualType.DefaultMember;
                if ( memberBase == null ||
                     member == null ||
                     !UnionProperty((CProperty)memberBase, (CProperty) member))
                {
                    hasDefault = false;
                    break;
                }
            }
            if (hasDefault)
                DefaultMember = ((CClass)types[0]).DefaultMember;

            return null;
        }

        private static bool UnionProperty(CProperty propo, CProperty luPropo)
        {
            for (int i = 0; i < 3; i++)
            {
                if ((propo.Declared[i] == null) != (luPropo.Declared[i] == null))
                    return false;
                else if (propo.Declared[i] != null &&
                    ((CFunction)propo.Declared[i]).Arguments.Count != ((CFunction)luPropo.Declared[i]).Arguments.Count)
                    return false;
            }
            return true;
        }

        public List<CTypeRef> Types
        {
            get { return types; }
            set { types = value; }
        }

        protected override bool canUnionConvert(CClass klass)
        {
            foreach (CTypeRef tref in types)
            {
                if (tref.ActualType == null || !tref.ActualType.canConvertTo(klass))
                    return false;
            }
            return true;
        }

        public override bool IsEnum
        {
            get
            {
                foreach (CTypeRef tref in types)
                {
                    if (!tref.Resolved || !tref.ActualType.IsEnum)
                        return false;
                }
                return true;
            }
        }

        public override bool IsInterface
        {
            get
            {
                foreach (CTypeRef tref in types)
                {
                    if (!tref.Resolved || !tref.ActualType.IsInterface)
                        return false;
                }
                return true;
            }
        }
    }
}
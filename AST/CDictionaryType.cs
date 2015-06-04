using System;
using System.Collections.Generic;
using System.Collections;

namespace FogCreek.Wasabi.AST
{
    public class CDictionaryType : CArrayType
    {
        private bool generatingMembers;
        private bool generatedMembers;

        protected override string NamePrefix
        {
            get { return "Dictionary"; }
        }

        protected override string Open { get { return "{"; } }
        protected override string Close { get { return "}"; } }

        public override bool CanBeRedimed
        {
            get { return false; }
        }

        public static List<CDictionaryType> Dictionaries = new List<CDictionaryType>();

        public CDictionaryType(CTypeRef type)
            : base(type, 1)
        {
            indexType.InternalLoad(BuiltIns.String);
            Dictionaries.Add(this);
            IsObject = true;
        }

        public override CTypeRef ItemType
        {
            get { return base.ItemType; }
            set
            {
                base.ItemType = value;

                if (value.Resolved && generatedMembers)
                    UpdateMembers();
            }
        }

        private Mono.Cecil.MethodDefinition FindMethod(string name)
        {
            foreach (Mono.Cecil.MethodDefinition method in CecilType.Methods)
            {
                if (method.Name == name)
                    return method;
            }
            throw new ArgumentOutOfRangeException("name");
        }

        private void UpdateMembers()
        {
            CMethod items = (CMethod)base.LookupMember("items");
            ((CArrayType)items.Function.Type.ActualType).ItemType = ItemType;

            CMethod add = (CMethod)base.LookupMember("add");
            add.Function.Arguments[1].LoadType(ItemType);

            CProperty item = (CProperty)base.LookupMember("item");
            item.LoadType(ItemType);
            if (item.GetAccessor != null)
                item.GetAccessor.LoadType(ItemType);
            if (item.SetAccessor != null)
                item.SetAccessor.Arguments[1].LoadType(ItemType);
        }

        public override IEnumerable<CMember> DirectMemberIterator
        {
            get
            {
                if (!generatingMembers)
                {
                    GenerateAndResolveMembers();
                }
                return base.DirectMemberIterator;
            }
        }

        public override void SetMember(string k, CMember v)
        {
            if (!generatingMembers)
            {
                GenerateAndResolveMembers();
            }
            base.SetMember(k, v);
        }

        public override CMember LookupMember(string k)
        {
            if (!generatingMembers)
            {
                GenerateAndResolveMembers();
            }
            return base.LookupMember(k);
        }

        private void TryItemTypeFixup()
        {
            CClass type;
            CMethod items = (CMethod)base.LookupMember("items");

            type = ((CArrayType)items.Declared[CProperty.ixGet].Type).ItemType.ActualType;

            if (type == null)
            {
                CMethod add = (CMethod)base.LookupMember("add");
                type = add.Function.Arguments[1].Type.ActualType;
            }

            if (type == null)
            {
                CProperty item = (CProperty)base.LookupMember("item");
                type = item.Type.ActualType;
                if (type == null && item.GetAccessor != null)
                    type = item.GetAccessor.Type.ActualType;
                if (type == null && item.SetAccessor != null)
                    type = item.SetAccessor.Arguments[1].Type.ActualType;
            }

            if (type != null)
                ItemType = new CTypeRef(null, type);
        }

        public override bool canConvertTo(CClass klass)
        {
            if (klass.Name == "progid:scripting.dictionary")
                return true;
            return base.canConvertTo(klass);
        }

        public override CMember DefaultMember
        {
            get { return LookupMember("item"); }
            set { throw new NotImplementedException(); }
        }


        private void GenerateMembers()
        {
            generatingMembers = true;
            
            // <snip>

            CMethod items = (CMethod)LookupMember("items");
            CClass type = new CArrayType(1);
            items.LoadType(type);
            items.Function.LoadType(type);

            base.Scope.Clear();
            foreach (CMember member in InheritedMemberIterator)
            {
                base.Scope.add(member);
            }

            generatingMembers = false;
            generatedMembers = true;
        }

        public override void Accept(IVisitor visitor)
        {
            GenerateAndResolveMembers();

            base.Accept(visitor);

            GenerateAndResolveMembers();
        }

        public override CScope Scope
        {
            get
            {
                GenerateAndResolveMembers();
                return base.Scope;
            }
        }

        private void GenerateAndResolveMembers()
        {
            if (!generatedMembers)
            {
                GenerateMembers();
                if (ItemType.Resolved)
                    UpdateMembers();
            }
            else if (!ItemType.Resolved)
                TryItemTypeFixup();
        }
    }
}
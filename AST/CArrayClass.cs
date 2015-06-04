using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CArrayType : CClass
    {
        private static int i = 1;

        private int dims;
        protected CTypeRef indexType;
        private int id;
        private CTypeRef itemType;

        protected virtual string NamePrefix
        {
            get { return "Array"; }
        }

        public override CAttributeList Attributes
        {
            get
            {
                CAttributeList cal = base.Attributes;
                if (!cal.contains("ExecuteAnywhere") && !cal.contains("ExecuteOnClient") && !cal.contains("ExecuteOnServer") && !cal.contains("ExecuteAtCompiler"))
                    cal.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(this, CProgram.Global.FindClass("ExecuteAnywhereAttribute")));
                return cal;
            }
        }

        protected virtual string Open { get { return "("; } }
        protected virtual string Close { get { return ")"; } }

        public virtual CTypeRef ItemType
        {
            get { return itemType; }
            set
            {
                itemType.InternalLoad(value);
                if (itemType.Resolved)
                    SetSemanticallyComplete();

                if (ReferenceEquals(value.ActualType, this))
                    throw new InvalidOperationException("Array ItemType circular reference");

                
                string name = (itemType.Resolved ? "" : NamePrefix)
                    + (itemType.TypeName != null ? itemType.TypeName.RawValue : id.ToString())
                    + Open + new String(',', dims-1) + Close;
                NameToken = CToken.Identifer(NameToken, name, name);

                foreach (CArrayType link in linked)
                {
                    if (link.ItemType.Resolved && link.ItemType != this.ItemType)
                        throw new InvalidOperationException("IndexedType missinferred");

                    if (!link.ItemType.Resolved)
                        link.itemType = value;
                }
            }
        }

        public virtual bool CanBeRedimed
        {
            get { return true; }
        }

        public CTypeRef IndexType
        {
            get { return indexType; }
        }

        public int Dimensions
        {
            get { return dims; }
        }

        public CArrayType(int dims)
            : base("Array<" + (++i) + "," + dims + ">", false)
        {
            id = i;

            this.dims = dims;
            IsObject = false;
            indexType = new CTypeRef(this, BuiltIns.Int32);
            itemType = new CTypeRef(this);

            if (Compiler.Current != null)
                CProgram.Global.AddArray(this);
        }

        public override CTypeRef BaseClass
        {
            get
            {
                if (NamePrefix == "Array" && (!base.BaseClass.Resolved || base.BaseClass.ActualType == BuiltIns.Variant))
                    base.ForceSetBaseClass(CProgram.Global.FindClass("System.Array"));
                return base.BaseClass;
            }
        }

        public CArrayType(CTypeRef type, int dims)
            : this(dims)
        {
            ItemType = type;
        }

        protected override bool canArrayConvert(CArrayType klass)
        {
            if (IndexType == klass.IndexType && ItemType.Resolved)
                return ItemType.ActualType.canConvertTo(klass.ItemType);
            return false;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is CArrayType))
                return false;

            if (ReferenceEquals(obj, this))
                return true;

            CArrayType other = (CArrayType)obj;

            if (NamePrefix != other.NamePrefix)
                return false;

            if (!itemType.Resolved || !other.itemType.Resolved)
                return false;

            if (!indexType.Equals(other.indexType))
                return false;

            if (dims != other.dims)
                return false;

            return itemType.Equals(other.itemType);
        }

        public override int GetHashCode()
        {
            return 123456789;
        }

        List<CArrayType> linked = new List<CArrayType>();
        internal void Link(CArrayType aFrom)
        {
            linked.Add(aFrom);
        }
    }
}
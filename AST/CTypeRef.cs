using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FogCreek.Wasabi.AST
{
    [DebuggerDisplay("TypeName: {TypeName}, ActualType: {ActualType}")]
    public struct CTypeRef
    {
        private readonly CNode owner;
        private CToken name;
        private CClass type;

        public static readonly CTypeRef Empty = new CTypeRef(null);

        public CTypeRef(CNode owner)
        {
            this.owner = owner;
            name = null;
            type = null;
        }

        public CTypeRef(CNode owner, CClass type)
            : this(owner)
        {
            InternalLoad(type);
        }

        public CTypeRef(CNode owner, CToken name)
            : this(owner)
        {
            InternalLoad(name);
        }

        [Obsolete("Only used by generators")]
        public CTypeRef(CNode owner, string name) : this(owner)
        {
            InternalLoad(CToken.Identifer(null, name));
        }

        public CTypeRef(CNode owner, CTypeRef tref)
            : this(owner)
        {
            InternalLoad(tref);
        }

        public CNode Owner
        {
            get { return owner; }
        }

        internal void InternalLoad(CTypeRef tref)
        {
            TypeName = tref.name;
            ActualType = tref.type;
        }

        internal void InternalLoad(CClass type)
        {
            ActualType = type;
        }

        internal void InternalLoad(CToken name)
        {
            TypeName = name;
            ActualType = null;
        }

        public CToken TypeName
        {
            get
            {
                if (Resolved) return type.NameToken;
                return name;
            }
            private set { name = value; }
        }

        public string RawName
        {
            get { return type.RawName; }
        }

        public bool Resolved
        {
            get { return type != null; }
        }

        public CClass ActualType
        {
            get { return type; }
            private set
            {
                if (type != value)
                {
                    type = value;
                    if (owner != null) owner.DoTypeChanged();
                }
            }
        }

        public static implicit operator CClass(CTypeRef tref)
        {
            return tref.ActualType;
        }

        public override bool Equals(object obj)
        {
            return this == (CTypeRef)obj;
        }

        public bool Equals(CTypeRef obj)
        {
            return this == obj;
        }

        public static bool operator ==(CTypeRef l, CTypeRef r)
        {
            if (!l.Resolved || !r.Resolved)
            {
                // allow comparisons to Empty
                if (l.name == null || r.name == null)
                {
                    if (l.Resolved || r.Resolved)
                        return false;
                    return l.name == r.name;
                }

                throw new InvalidOperationException("Cannot compare unresolved types");
            }
            return l.ActualType == r.ActualType;
        }

        public static bool operator !=(CTypeRef l, CTypeRef r)
        {
            return !(l == r);
        }

        public override int GetHashCode()
        {
            if (this.Resolved)
                return this.ActualType.GetHashCode();
            if (this.name != null)
                return this.name.GetHashCode();
            return base.GetHashCode();
        }
    }
}
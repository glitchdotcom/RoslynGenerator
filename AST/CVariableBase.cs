using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public abstract class CVariableBase : CNode, IVariable, IAttributed
    {
        protected CVariableBase(CToken token, bool shared)
            : base(token)
        {
            this.shared = shared;
        }

        protected CVariableBase(CToken token)
            : this(token, false)
        {
        }

        protected CToken name;
        private CAttributeList attribs = new CAttributeList();
        protected bool accessedBeforeUsed = false;
        protected int assignCount = 0;
        protected int accessCount = 0;
        private CClass containingClass;
        private CFunction containingFunction;
        private bool shared = false;
        private bool external = false;

        public virtual CToken Name
        {
            get { return name; }
        }

        String IVariable.Name
        {
            get { return name.Value; }
        }

        public CClass ContainingClass
        {
            get { return containingClass; }
            set { containingClass = value; }
        }

        public CFunction ContainingFunction
        {
            get { return containingFunction; }
            set { containingFunction = value; }
        }

        public virtual bool AccessedBeforeUsed
        {
            get { return accessedBeforeUsed; }
        }

        public virtual int AssignmentCount
        {
            get { return assignCount; }
        }

        public virtual int AccessCount
        {
            get { return accessCount; }
        }

        string IVariable.RawName
        {
            get { return name.RawValue; }
        }

        public bool IsArray
        {
            get { return Type.ActualType is CArrayType && !(Type.ActualType is CDictionaryType); }
        }

        public string TypeName
        {
            get
            {
                if (Type.TypeName != null)
                    return Type.TypeName.Value;
                return null;
            }
        }

        public CAttributeList Attributes
        {
            get { return attribs; }
        }

        public abstract bool IsField { get; set; }

        public bool IsShared
        {
            get { return shared; }
        }

        public virtual void incAssignmentCount(CClass currentclass, CFunction currentfunction)
        {
            assignCount++;
        }

        public virtual void incAccessCount(CClass currentclass, CFunction currentfunction)
        {
            if (assignCount == 0)
                accessedBeforeUsed = true;
            accessCount++;
        }

        public virtual bool canAssign(CClass currentclass, CFunction currentfunction)
        {
            return true;
        }

        public virtual void ConvertToArray(CClass type, int count)
        {
            base.LoadType(type);
        }

        public bool IsExternallyReferenced
        {
            get { return external; }
        }

        public void SetExternallyReferenced()
        {
            external = true;
        }
    }
}
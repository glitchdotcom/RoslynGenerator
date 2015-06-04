using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CVariable : CVariableBase, INodeParent
    {
        private CParameters arrayDimsinit;
        private CExpression init;
        private bool member;
        private CNode rootForRedim;
        private CField field;
        private CDim dim;
        private Mono.Cecil.FieldDefinition cecilField;
        private bool firstAccessIsRedimPreserve = false;

        public CVariable(CToken name, bool shared, CTypeRef tref, CParameters arrayDimsinit, CExpression init, CDim parent)
            : base(name, shared)
        {
            this.dim = parent;
            this.name = name;
            base.LoadType(tref);
            this.init = init;
            if (init != null)
                init.Parent = this;
            member = false;
            this.arrayDimsinit = arrayDimsinit;
        }

        public override void LoadType(CClass type)
        {
            base.LoadType(type);

            EnsureDiminsionInitializerIsValid();
        }

        public override void LoadType(CTypeRef tref)
        {
            base.LoadType(tref);

            EnsureDiminsionInitializerIsValid();
        }

        public bool FirstAccessIsRedimPreserve
        {
            get { return firstAccessIsRedimPreserve; }
            set { firstAccessIsRedimPreserve = value; }
        }

        public override bool IsField
        {
            get { return member; }
            set { member = value; }
        }


        public CDim Dim
        {
            get { return this.dim; }
        }

        public CNode RootForRedim
        {
            get { return rootForRedim; }
            set { rootForRedim = value; }
        }

        public CExpression Initializer
        {
            get { return init; }
            protected set
            {
                init = value;
                if (init != null)
                    init.Parent = this;
            }
        }

        public CParameters DimensionInitializer
        {
            get { return arrayDimsinit; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitVariable(this);
        }

        public override void ConvertToArray(CClass type, int count)
        {
            base.ConvertToArray(type, count);

            EnsureDiminsionInitializerIsValid();
        }

        private void EnsureDiminsionInitializerIsValid()
        {
            CDictionaryType dict = Type.ActualType as CDictionaryType;
            if (dict != null)
                return;

            CArrayType array = Type.ActualType as CArrayType;
            if (array == null || arrayDimsinit != null)
                return;

            int count = array.Dimensions;
            CParameters @params = new CParameters();
            for (int i = 0; i < count; i++)
                @params.Unnamed.Add(null);

            arrayDimsinit = @params;
        }

        public Mono.Cecil.FieldDefinition CecilField
        {
            get { return cecilField; }
            set { cecilField = value; }
        }

        public CField Field
        {
            get { return field; }
            set { field = value; }
        }

        #region INodeParent Members

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == Initializer)
                Initializer = (CExpression)newchild;
        }

        #endregion
    }
}
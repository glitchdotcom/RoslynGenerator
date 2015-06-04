using System;

namespace FogCreek.Wasabi.AST
{
    public class CAttribute : CNode
    {
        private readonly string name;
        private readonly CToken nameToken;
        private readonly CParameters parameters;

        public CAttribute(CToken name, CParameters parameters, CTypeRef ctr)
            : base(name)
        {
            this.name = name.Value;
            nameToken = name;
            this.parameters = parameters ?? new CParameters();
            LoadType(new CTypeRef(this, ctr));
        }

        public string Name
        {
            get { return name; }
        }

        public CToken NameToken
        {
            get { return nameToken; }
        }

        public CParameters Parameters
        {
            get { return parameters; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitAttribute(this);
        }
    }
}
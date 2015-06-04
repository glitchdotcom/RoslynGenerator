using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CNew : CAccess
    {
        private CToken classname;
        private CParameters @params;

        public CNew(CToken tok, CToken classname, CParameters @params)
            : base(tok, classname)
        {
            this.classname = classname;
            this.@params = @params;
            this.@params.Parent = this;
        }

        public CParameters Parameters
        {
            get { return @params; }
        }

        public CToken ClassName
        {
            get { return classname; }
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitNew(this);
        }
    }
}
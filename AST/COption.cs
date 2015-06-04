using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class COption : CStatement
    {
        private CToken name;
        private CToken param;
        private CFile file;

        public COption(CToken tok, CToken name, CToken param)
            : base(tok)
        {
            this.name = name;
            this.param = param;
        }

        public CToken Name
        {
            get { return name; }
        }

        public CToken Parameter
        {
            get { return param; }
        }

        public CFile File
        {
            get { return file; }
            set { file = value; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitOption(this);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace FogCreek.Wasabi.AST
{
    [DebuggerDisplay("Name: {RawName}")]
    public class CEnum : CClass
    {
        public CEnum(CToken tok, CToken name)
            : base(tok, name.RawValue, name.Value, CTypeRef.Empty)
        {
            IsObject = false;
            IsNumeric = true;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitEnum(this);
        }

        internal override void AddInterface(CTypeRef interfaceref)
        {
            throw new InvalidOperationException("Enums can't have interfaces");
        }
    }
}

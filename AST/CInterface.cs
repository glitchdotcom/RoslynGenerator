using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace FogCreek.Wasabi.AST
{
    [DebuggerDisplay("Name: {RawName}")]
    public class CInterface : CClass
    {

        List<CClass> genericParameters = new List<CClass>();

        public CInterface(CToken tok, CToken name) : base(tok, name.RawValue, name.Value, CTypeRef.Empty) { }

        public List<CClass> GenericParameters { get { return genericParameters; } }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitInterface(this);
        }

        public override CMember LookupMember(string k)
        {
            CMember baseMember = base.LookupMember(k);
            if (baseMember == null)
            {
                foreach (CInterface iface in this.Interfaces)
                {
                    CMember lum = iface.LookupMember(k);
                    if (lum != null)
                        return lum;
                }
            }
            return baseMember;
        }
    }
}

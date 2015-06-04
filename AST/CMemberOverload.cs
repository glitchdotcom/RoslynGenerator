using System;
using System.Collections.Generic;
using System.Text;
using FogCreek.Wasabi.AST;

namespace FogCreek.Wasabi.AST
{
    public class CMemberOverload : CMember
    {
        CClass owner;
        List<CMember> overloads = new List<CMember>();

        public CMemberOverload(CToken tok, CClass owner, string name)
            : base(tok, name, "override", 0, false)
        {
            this.owner = owner;
        }

        public CMemberOverload(CMemberOverload field, bool isUnionMember)
            : base(field.Token, field.Name, "override", 0, isUnionMember)
        {
            this.owner = field.DeclaringClass;
        }

        public void Add(CMember member)
        {
            if (member is CMemberOverload)
            {
                foreach (CMember m in ((CMemberOverload)member).overloads)
                    Add(m);
            }
            else
                overloads.Add(member);
        }

        public CClass Owner
        {
            get { return owner; }
        }

        public override IEnumerable<CMember> Overloads
        {
            get
            {
                return overloads;
            }
        }

        public override TokenTypes Visibility
        {
            get
            {
                TokenTypes vis = overloads[0].Visibility;
                foreach (CMember m in overloads)
                    if (m.Visibility != vis)
                        throw new NotImplementedException("Mixed visibility in member overloads");
                return vis;
            }
        }

        public override CClass DeclaringClass
        {
            get { return owner; }
        }
    }
}

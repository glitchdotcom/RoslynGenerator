using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CMemberAccess : CAccess, INodeParent
    {
        private CAccess _object;
        private CMember member;

        public CMemberAccess(CToken tok, CAccess objectSource, CToken item)
            : base(tok, item)
        {
            _object = objectSource;
            _object.Parent = this;
            _object.IsMemberSource = true;
            IsRootAccess = false;
        }

        public CAccess MemberSource
        {
            get { return _object; }
        }

        public CMember ReferencedMember
        {
            get { return member; }
            set { member = value; }
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitMemberAccess(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == _object)
                _object = (CAccess)newchild;
            newchild.Parent = this;
        }
    }
}
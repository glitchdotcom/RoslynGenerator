using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CAccess : CExpression
    {
        private CToken referenceToken;
        private bool isRootAccess;
        private bool isMemberSource;
        private bool isCallExplicit;
        private bool isCallImplicit;
        private CNode referenceTarget;


        public CAccess(CToken tok, CToken item)
            : base(tok)
        {
            referenceToken = item;
            isMemberSource = false;
            isRootAccess = true;
        }

        public bool IsRootAccess
        {
            get { return isRootAccess; }
            protected set { isRootAccess = value; }
        }

        public bool IsMemberSource
        {
            get { return isMemberSource; }
            set { isMemberSource = value; }
        }

        public CNode ReferenceTarget
        {
            get { return referenceTarget; }
            set { referenceTarget = value; }
        }

        public CToken ReferenceToken
        {
            get { return referenceToken; }
            protected set { referenceToken = value; }
        }

		/// <summary>
		/// Both IsCallExplicit and IsCallImplicit exist. However, they are not mutually exclusive opposites, so they cannot
		/// be condensed down into one method.
		/// </summary>
        public bool IsCallExplicit
        {
            get { return isCallExplicit; }
            set { isCallExplicit = value; }
        }

		/// <summary>
		/// Both IsCallExplicit and IsCallImplicit exist. However, they are not mutually exclusive opposites, so they cannot
		/// be condensed down into one method.
		/// </summary>
        public bool IsCallImplicit
        {
            get { return isCallImplicit; }
            set { isCallImplicit = value; }
        }

        public override void Accept(IVisitor visit)
        {
            visit.VisitAccess(this);
        }

        public override bool IsConstant
        {
            get
            {
                return (ReferenceTarget is CConst) || (ReferenceTarget is CClassConst) || ((ReferenceTarget is CExpression) && ((CExpression)ReferenceTarget).IsConstant);
            }
        }

        public bool ForceGlobalAccess
        {
            get;
            set;
        }
    }
}

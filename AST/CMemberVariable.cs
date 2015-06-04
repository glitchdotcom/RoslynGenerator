using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CMemberVariable : CDim, IHasVisibility
    {
        private TokenTypes visibility;

        /// <summary>Creates a new instance of CMemberVariable </summary>
        // ppub = 0, priv =1 as defined in token types interface
        public CMemberVariable(CToken token, TokenTypes visibility)
            : base(token)
        {
            this.visibility = visibility;
        }

        public TokenTypes Visibility
        {
            get { return visibility; }
            internal set { visibility = value; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitMemberVariable(this);
        }

        #region IHasVisibility Members

        public CClass DeclaringClass
        {
            get { return this.Variables[0].ContainingClass; }
        }

        bool isStatic = false;
        public bool IsStatic
        {
            get { return isStatic; }
            set { isStatic = value; }
        }

        #endregion
    }
}
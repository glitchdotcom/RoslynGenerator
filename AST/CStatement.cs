using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    // all of the other classes which handle the different kinds of statements in 
    // the vbs language all inherit from this class
    public abstract class CStatement : CNode
    {
        private bool hasInlineComment = false;

        protected internal CStatement(CToken token)
            : base(token)
        {
        }

        public bool HasInlineComment
        {
            get { return hasInlineComment; }
            set { hasInlineComment = value; }
        }

        public abstract override void Accept(IVisitor visitor);
    }
}
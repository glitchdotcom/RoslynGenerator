using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    // this class is for statements in the original vb code that are assignment statements.
    // assignment statements have a left hand side and a right hand side
    public class CAssignment : CStatement, INodeParent
    {
        private CAccess lhs; // the left hand side of the assignment statement
        private CExpression rhs; // the right hand side of it

        // this is a special case from the normal object types.  we are actually being a given an
        // object of type CExpression to parse rather than just the lex stream.
        public CAssignment(CToken token)
            : base(token) {}

        public CAccess Target
        {
            get { return lhs; }
            set
            {
                lhs = value;
                lhs.Parent = this;
                lhs.LhsAssignmentTarget = true;
            }
        }

        public CExpression Source
        {
            get { return rhs; }
            set
            {
                rhs = value;
                rhs.Parent = this;
                rhs.RhsAssignmentSource = true;
            }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (lhs == child)
                lhs = (CAccess)newchild;
            if (rhs == child)
                rhs = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
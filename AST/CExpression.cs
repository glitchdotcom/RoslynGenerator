using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public abstract class CExpression : CNode
    {
        private bool lhsAssignmentTarget;
        private bool rhsAssignmentSource;
        private bool passedByRef = false;
        private CArgument referencedArgument;

        public CExpression(CToken token)
            : base(token)
        {
        }

        public virtual bool IsConstant
        {
            get { return false; }
        }

        public bool IsPassedByRef
        {
            get { return passedByRef; }
            set { passedByRef = value; }
        }

        public bool LhsAssignmentTarget
        {
            get { return lhsAssignmentTarget; }
            set { lhsAssignmentTarget = value; }
        }

        public bool RhsAssignmentSource
        {
            get { return rhsAssignmentSource; }
            set { rhsAssignmentSource = value; }
        }

        public CArgument ReferencedArgument
        {
            get { return referencedArgument; }
            set { referencedArgument = value; }
        }
    }
}
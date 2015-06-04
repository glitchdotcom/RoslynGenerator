using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    // vbcscipt for loop statement
    public class CFor : CStatement, INodeParent
    {
        private CAccess forVariable; // like i, or j, or counter
        private CExpression init; // the initial value of the var
        private CExpression condition; // the final value of the var
        private CExpression step; // the expression to add to the var
        private CStatementBlock statements = new CStatementBlock();

        public CFor(CToken token, CToken var)
            : base(token)
        {
            forVariable = new CAccess(var, var);
            forVariable.Parent = this;
        }

        public CExpression Initializer
        {
            get { return init; }
            set
            {
                init = value;
                init.Parent = this;
            }
        }

        public CExpression Terminator
        {
            get { return condition; }
            set
            {
                condition = value;
                condition.Parent = this;
            }
        }

        public CExpression Step
        {
            get { return step; }
            set
            {
                step = value;
                step.Parent = this;
            }
        }

        [Obsolete("Use ForVariable instead")]
        public string ForVariableName
        {
            get { return ForVariableToken.Value; }
        }

        [Obsolete("Use ForVariable instead")]
        public CToken ForVariableToken
        {
            get { return forVariable.Token; }
        }

        public CAccess ForVariable
        {
            get { return forVariable; }
        }

        public CStatementBlock Statements
        {
            get { return statements; }
            set { statements = value; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitFor(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == init)
                init = (CExpression)newchild;
            if (child == condition)
                condition = (CExpression)newchild;
            if (child == step)
                step = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
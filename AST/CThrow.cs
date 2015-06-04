using System;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public class CThrow : CStatement, INodeParent
    {
        CExpression expression;

        public CExpression Expression
        {
            get { return expression; }
            set
            {
                this.expression = value;
                value.Parent = this;
            }
        }

        /// <summary>
        /// Create a new Throw statement
        /// </summary>
        /// <param name="token">The token defining the Throw</param>
        /// <param name="expression">The expression, if any, being thrown (may be null)</param>
        public CThrow(CToken token)
            : base(token) { }

        public void Replace(CNode child, CNode newchild)
        {
            if (child == expression)
                Expression = (CExpression)newchild;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitThrow(this);
        }
    }
}

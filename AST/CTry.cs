using System;
using System.Collections.Generic;
using System.Text;

namespace FogCreek.Wasabi.AST
{
    public class CTry : CStatement
    {
        CStatementBlock statements = new CStatementBlock();
        CStatementBlock catchBlocks = new CStatementBlock();
        CFinally finallyBlock;

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public CStatementBlock CatchBlocks
        {
            get { return catchBlocks; }
        }

        public CFinally FinallyBlock
        {
            get { return finallyBlock; }
            set { finallyBlock = value; }
        }

        /// <summary>
        /// Create a new Try-Catch-Finally block.
        /// </summary>
        /// <param name="token">The token defining the Try</param>
        public CTry(CToken token)
            : base(token) { }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitTry(this);
        }
    }
}

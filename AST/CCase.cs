using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    // this class is for the specific cases of a switch statement 
    public class CCase : CStatement, INodeParent
    {
        private CExpression m_value; // in the line case('prephase') then 'prephase' is the vaue
        private CStatementBlock statements; // the statements to be executed in this case of the switch
        private bool elseCaseFlag; // flag indicating if this case is the default case


        // if statements is null, that means there are no statements.  meaning this case uses the statements
        // of the case below it.  so dont put a break point.  eg:
        //  switch (color) {
        //      case (red):
        //      case (blue):
        //          print("its red or blue);
        //          break;
        //
        public CCase(CToken token, CExpression val)
            : base(token)
        {
            m_value = val;
            m_value.Parent = this;
            statements = null;
        }

        // this is the constructor when you actually need to parse all the statements of a case
        // until you hit the next case
        public CCase(CToken token, CExpression val, CStatementBlock block)
            : base(token)
        {
            m_value = val;
            elseCaseFlag = val == null;
            if (!elseCaseFlag)
                m_value.Parent = this;
            statements = block;
        }

        public bool IsElseCase
        {
            get { return elseCaseFlag; }
        }

        public CStatementBlock Statements
        {
            get { return statements; }
        }

        public CExpression Value
        {
            get { return m_value; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitCase(this);
        }

        void INodeParent.Replace(CNode child, CNode newchild)
        {
            if (child == m_value)
                m_value = (CExpression)newchild;
            newchild.Parent = this;
        }
    }
}
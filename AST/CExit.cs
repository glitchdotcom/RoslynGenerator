using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    // the exit statement in vbscript
    public class CExit : CStatement
    {
        private bool subFlag;
        private bool funcFlag;
        private String exitWhat;

        public CExit(CToken token, String exitWhat)
            : base(token)
        {
            this.exitWhat = exitWhat;
            if (exitWhat == "function")
                funcFlag = true;
            else if (exitWhat == "sub")
                subFlag = true;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitExit(this);
        }

        public bool IsExitSub
        {
            get { return subFlag; }
            set { subFlag = value; }
        }

        public bool IsExitFunction
        {
            get { return funcFlag; }
            set { funcFlag = value; }
        }

        public String ExitType
        {
            get { return exitWhat; }
            set { exitWhat = value; }
        }
    }
}
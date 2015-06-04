using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CLambdaExpression : CExpression
    {
        private static int lambdaCount = 0;

        private int lambdaId;
        private CLambdaFunction lambdaFunction;
        private CFunctionType lambdaType;

        public CLambdaExpression(CToken token)
            : base(token)
        {
            lambdaId = lambdaCount++;
        }

        public CLambdaFunction LambdaFunction
        {
            get { return lambdaFunction; }
        }

        public CFunctionType LambdaType
        {
            get { return lambdaType; }
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitLambdaExpression(this);
        }

        public override bool SemanticallyComplete
        {
            get { return lambdaType.SemanticallyComplete; }
        }

        internal CStatementBlock StartInitialize(CFunction containingFunction, CFile containingFile, CTypeRef tref, CArgumentList args)
        {
            if (lambdaFunction != null)
                throw new InvalidOperationException("Lambdas can only be initalized once");

            CClass @class = null;
            string extra = "";
            if (containingFunction != null)
            {
                @class = containingFunction.Class;
                extra += containingFunction.RawName;
            }

            lambdaFunction =
                new CLambdaFunction(Token, containingFunction, containingFile, "Lambda_" + extra + "_" + lambdaId, tref, args);
            base.LoadType(lambdaType = new CFunctionType(Token, lambdaFunction, false));

            lambdaFunction.Class = @class;

            return lambdaFunction.Statements;
        }

        internal void FinishInitialize(CExpression cExpression)
        {
            CReturn @return = new CReturn(Token);
            @return.Expression = cExpression;

            lambdaFunction.Statements.Add(@return);
        }
    }
}
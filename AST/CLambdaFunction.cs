using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CLambdaFunction : CFunction
    {
        private readonly CFunction containingFunction;
        private readonly CFile containingFile;
        private readonly List<IVariable> externalReferences = new List<IVariable>();

        public CLambdaFunction(CToken token, CFunction containingFunction, CFile containingFile, String name, CTypeRef tref,
                               CArgumentList args)
            : base(token, name, name, TokenTypes.visInternal, FunctionType.Function, args, tref)
        {
            this.containingFunction = containingFunction;
            this.containingFile = containingFile;
            if (this.containingFunction != null)
                this.containingFunction.Lambdas.Add(this);
            else
                this.containingFile.Lambdas.Add(this);
            CallCount++;
            Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(null, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
        }

        public CFunction ContainingFunction
        {
            get { return containingFunction; }
        }

        public CFile ContainingFile
        {
            get { return containingFile; }
        }

        /// <summary>
        /// The list of external references, that are not globals or fields
        /// </summary>
        public List<IVariable> ExternalReferences
        {
            get { return externalReferences; }
        }
    }
}
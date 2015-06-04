using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CMethod : CMember
    {
        private CFunction function;

        public CMethod(CFunction func)
            : base(func.Token, func.Name, "method", 1, false)
        {
            Declared[0] = func;
            function = func;
        }

        public CMethod(CMethod method, bool isUnionMember)
            : base(method.Token, method.Name, "method", 1, isUnionMember)
        {
            Declared[0] = function = method.function;
        }

        public CFunction Function
        {
            get { return function; }
        }

        /// <summary>
        /// Function is a reserved word in Wasabi, so we add an alias "FunctionObject".
        /// </summary>
        public CFunction FunctionObject
        {
            get { return function; }
        }

        public override TokenTypes Visibility
        {
            get { return function.Visibility; }
        }

        public override CClass DeclaringClass
        {
            get { return function.DeclaringClass; }
        }
    }
}
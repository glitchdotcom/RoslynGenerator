using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CFunctionType : CClass
    {
        private readonly CFunction target;
        private readonly bool declarationOnly;

        public CFunction Target
        {
            get { return target; }
        }

        public CFunctionType(CToken token, CFunction function, bool declarationOnly)
            : base(token, function.TypeSignature)
        {
            target = function;
            this.declarationOnly = declarationOnly;

            CMethod method = new CMethod(function);
            DefaultMember = method;

            function.TypeChanged += new EventHandler(function_TypeChanged);
            foreach (CArgument arg in function.Arguments)
                arg.TypeChanged += new EventHandler(function_TypeChanged);

            this.IsSealed = true;

            // we don't actually want people accessing the default method directly
            // we also don't want it to get visited through the type.
            // so we don't do this: Members.Add(function.Name, method);
            Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(null, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
        }

        private void function_TypeChanged(object sender, EventArgs e)
        {
            UpdateName();
            OnTypeChanged();
        }

        private void UpdateName()
        {
            string name = target.TypeSignature;
            NameToken = CToken.Identifer(NameToken, name, name);
        }

        public override bool canConvertTo(CClass klass)
        {
            CFunctionType target = klass as CFunctionType;
            if (target == null) return base.canConvertTo(klass);

            UpdateName();
            target.UpdateName();

            return Name == target.Name;
        }

        public static bool PossibleMatch(CFunctionType from, CFunctionType to)
        {
            if (from.Target.Arguments.Count != to.Target.Arguments.Count)
                return false;

            for (int i = 0; i < from.Target.Arguments.Count; i++)
            {
                if (from.Target.Arguments[i].Direction.Value != to.Target.Arguments[i].Direction.Value)
                    return false;

                if (from.Target.Arguments[i].Type.Resolved && to.Target.Arguments[i].Type.Resolved
                    && from.Target.Arguments[i].Type.ActualType != to.Target.Arguments[i].Type.ActualType)
                    return false;                
            }

            return true;
        }

        public override bool SemanticallyComplete
        {
            get
            {
                if (target.SemanticallyComplete && base.SemanticallyComplete)
                    return true;

                if (!target.Type.Resolved)
                    return false;
                foreach (CArgument arg in target.Arguments)
                    if (!arg.Type.Resolved)
                        return false;
                if (declarationOnly || target.Statements.SemanticallyComplete)
                {
                    base.SetSemanticallyComplete();
                    return true;
                }

                return false;
            }
        }
    }
}
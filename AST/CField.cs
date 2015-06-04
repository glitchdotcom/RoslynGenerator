using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CField : CMember
    {
        private CMemberVariable declaration;
        private CVariable variable;

        public CField(CVariable var, CMemberVariable declaration)
            : base(var.Token, var.Name.Value, "field", 1, false)
        {
            Declared[0] = var;
            variable = var;
            var.Field = this;
            this.declaration = declaration;
        }

        public CField(CField field, bool isUnionMember)
            : base(field.Token, field.Name, "field", 1, isUnionMember)
        {
            this.declaration = field.declaration;
            Declared[0] = this.variable = field.variable;
        }

        public override bool SemanticallyComplete
        {
            get { return declaration.SemanticallyComplete; }
        }

        public override TokenTypes Visibility
        {
            get { return declaration.Visibility; }
        }

        public override CClass DeclaringClass
        {
            get { return declaration.DeclaringClass; }
        }

        public CMemberVariable MemberDeclaration
        {
            get { return declaration; }
            set { declaration = value; }
        }

        public CVariable Variable
        {
            get { return variable; }
            set { variable = value; }
        }
    }
}
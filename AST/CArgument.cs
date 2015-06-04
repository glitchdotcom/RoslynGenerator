using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CArgument : CVariableBase, IVariable
    {
        private CToken direction;

        private Mono.Cecil.ParameterDefinition cecilParameter;

        private bool indexerValueArgument = false;

        public bool IndexerValueArgument
        {
            get { return indexerValueArgument; }
            set { indexerValueArgument = value; }
        }

        public Mono.Cecil.ParameterDefinition CecilParameter
        {
            get { return cecilParameter; }
            set { cecilParameter = value; }
        }

        public CArgument(CToken direction, CToken name, CTypeRef tref)
            : base(name)
        {
            this.direction = direction;
            this.name = name;
            base.LoadType(tref);
        }

        public bool Optional
        {
            get { return Attributes.contains("optional"); }
        }

        // Optional is a reserved word in Wasabi
        public bool IsOptional
        {
            get { return Optional; }
        }

        public CExpression DefaultValue
        {
            get
            {
                if (Attributes.contains("optional"))
                {
                    CAttribute attrib = Attributes["optional"];
                    if (attrib.Parameters.Unnamed.Count != 0)
                        return (CExpression)attrib.Parameters[0];
                }
                return null;
            }
        }

        public CToken Direction
        {
            get { return direction; }
        }

        public override bool IsField
        {
            get { return false; }
            set { throw new NotImplementedException(); }
        }

        public override void Accept(IVisitor visitor)
        {
        }

        public string ToArgumentString()
        {
            string s1 = "";
            if (Optional) s1 = "Optional ";
            string s2 = "";
            if (Direction.Value == "byref")
                s2 = "ByRef ";
            else if (Direction.Value == "byval")
                s2 = "ByVal ";
            string s3 = "";
            if (Type != null && Type.TypeName != null && !String.IsNullOrEmpty(Type.TypeName.RawValue))
            {
                string sName = Type.TypeName.RawValue;
                string sParens = "";
                while (sName.EndsWith("()"))
                {
                    sParens += "()";
                    sName = sName.Substring(0, sName.Length - 2);
                }
                s3 = sParens + " As [" + sName + "]";
            }
            return s1 + s2 + Name.RawValue + s3;
        }

        public string ToParameterString()
        {
            return Name.RawValue;
        }

        public string ToTypeString()
        {
            string sTypeName = Type.TypeName.RawValue;

            string sParens = "";
            while (sTypeName.EndsWith("()"))
            {
                sParens += "[]";
                sTypeName = sTypeName.Substring(0, sTypeName.Length - 2);
            }

            System.Type systype = System.Type.GetType("System." + sTypeName);
            if (systype != null)
                return systype.FullName + sParens;
            else
                return sTypeName + sParens;
        }
    }
}
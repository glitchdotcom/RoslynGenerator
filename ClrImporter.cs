using FogCreek.Wasabi.AST;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CustomAttributeCollection = Mono.Collections.Generic.Collection<Mono.Cecil.CustomAttribute>;

namespace FogCreek.Wasabi
{
    /// <summary>
    /// Just enough to be able to declare return types! Very incomplete!
    /// </summary>
    public class ClrImporter
    {
        public static void LoadBuiltins(CProgram program)
        {
            ImportType(program, SearchAssembliesForType(program, "System.Object"), BuiltIns.Variant);
            ImportType(program, SearchAssembliesForType(program, "System.String"), BuiltIns.String);

            CClass vtype = program.FindClass("System.ValueType");

            ImportType(program, SearchAssembliesForType(program, "System.Byte"), BuiltIns.Byte);
            BuiltIns.Byte.ForceSetBaseClass(vtype);
            ImportType(program, SearchAssembliesForType(program, "System.Int32"), BuiltIns.Int32);
            BuiltIns.Int32.ForceSetBaseClass(vtype);
            ImportType(program, SearchAssembliesForType(program, "System.Int64"), BuiltIns.Int64);
            BuiltIns.Int64.ForceSetBaseClass(vtype);
            ImportType(program, SearchAssembliesForType(program, "System.Char"), BuiltIns.Character);
            BuiltIns.Character.ForceSetBaseClass(vtype);
            ImportType(program, SearchAssembliesForType(program, "System.Boolean"), BuiltIns.Boolean);
            BuiltIns.Boolean.ForceSetBaseClass(vtype);
            ImportType(program, SearchAssembliesForType(program, "System.DateTime"), BuiltIns.Date);
            BuiltIns.Date.ForceSetBaseClass(vtype);
            ImportType(program, SearchAssembliesForType(program, "System.Double"), BuiltIns.Double);
            BuiltIns.Double.ForceSetBaseClass(vtype);

            BuiltIns.Variant.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.Variant, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            BuiltIns.Object.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.Object, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            BuiltIns.String.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.String, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            BuiltIns.Byte.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.Byte, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            BuiltIns.Int32.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.Int32, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            BuiltIns.Int64.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.Int64, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            BuiltIns.Character.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.Character, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            BuiltIns.Boolean.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.Boolean, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            BuiltIns.Date.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.Date, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            BuiltIns.Double.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(BuiltIns.Double, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
        }

        private static TypeDefinition SearchAssembliesForType(CProgram program, string name)
        {
            foreach (var asm in program.Assemblies)
            {
                var typed = asm.MainModule.Types.FirstOrDefault(td => td.FullName == name);
                if (typed != null)
                    return typed;
            }

            return null;
        }

        private static bool IsClsCompliant(CustomAttributeCollection customAttributeCollection)
        {
            foreach (CustomAttribute attr in FindAttributes(customAttributeCollection, "System.CLSCompliantAttribute"))
            {
                if (attr.ConstructorArguments.Count > 0)
                    return (bool)attr.ConstructorArguments[0].Value;
            }
            return true;
        }

        private static IEnumerable<CustomAttribute> FindAttributes(CustomAttributeCollection attrs, string name)
        {
            if (attrs != null)
                foreach (CustomAttribute attr in attrs)
                {
                    if (attr.Constructor.DeclaringType.FullName == name)
                        yield return attr;
                }
        }

        private static void ImportType(CProgram program, TypeDefinition typed, CClass type)
        {
            if (type.CecilType != null)
                return;
            type.CecilType = typed;

            if (typed.GenericParameters.Count > 0 || !IsClsCompliant(typed.CustomAttributes))
                return;


            if (typed.IsEnum)
            {
                type.IsObject = false;
                type.IsNumeric = true;
            }

            type.IsObject = !typed.IsValueType;

            type.SetSemanticallyComplete();
        }
    }
}

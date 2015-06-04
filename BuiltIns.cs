using System;
using System.Collections.Generic;
using System.Text;
using FogCreek.Wasabi.AST;

namespace FogCreek.Wasabi
{
    public static class BuiltIns
    {
        public static readonly CClass Byte;
        public static readonly CClass Int32;
        public static readonly CClass Int64;
        public static readonly CClass Character;
        public static readonly CClass String;
        public static readonly CClass Boolean;
        public static readonly CClass Date;
        public static readonly CClass Double;
        public static readonly CClass Object;
        public static readonly CClass Variant;
        public static readonly CClass DbNull;
        public static readonly CClass Nothing;
        public static readonly CClass Void;
        public static readonly CClass FunctionPlaceHolder;
        public static readonly CClass SubPlaceHolder;

        public static readonly CFunction Array;
        public static readonly CFunction Dictionary;
        public static readonly CFunction GetRef;

        public static readonly CClass SystemArray;

        static BuiltIns()
        {
            /* These aren't fully resolved; we need to use Cecil to load the appropriate .NET type defitinions in.
               See ClrImporter.LoadBuiltins. */
            Variant = new CClass("__Variant", null);
            Byte = new CClass("Byte", false);
            String = new CClass("String", true);
            Int32 = new CClass("Int32", true);
            Int64 = new CClass("Int64", true);
            Character = new CClass("Character", false);
            Boolean = new CClass("Boolean", true);
            Date = new CClass("Date", true);
            Double = new CClass("Double", true);
            Object = Variant;
            DbNull = new CClass("DbNull", false);
            Nothing = new CClass("Nothing", false);
            Nothing.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(null, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            Void = new CClass("__Void", false);
            FunctionPlaceHolder = new CClass("__FunctionPlaceHolder", false);
            FunctionPlaceHolder.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(null, CToken.Identifer(null, "ExecuteAnywhereAttribute")));
            SubPlaceHolder = new CClass("__SubPlaceHolder", false);
            SubPlaceHolder.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(null, CToken.Identifer(null, "ExecuteAnywhereAttribute")));

            SetupGlobalTypes();

            CArgumentList arrArgs = new CArgumentList();
            arrArgs.Add(new CArgument(CToken.Keyword(null, "byval"),
                CToken.Identifer(null, "arglist"),
                new CTypeRef(null, Variant)));
            Array = new CFunction(CToken.Identifer(null, "Array"), "Array", "array", TokenTypes.visPublic, FunctionType.Function,
                arrArgs, new CTypeRef(null, new CArrayType(new CTypeRef(null, Variant), 1)));
            Array.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(null, CToken.Identifer(null, "ExecuteAnywhereAttribute")));

            CArgumentList dicArgs = new CArgumentList();
            dicArgs.Add(new CArgument(CToken.Keyword(null, "byval"),
                CToken.Identifer(null, "arglist"),
                new CTypeRef(null, Variant)));
            Dictionary = new CFunction(CToken.Identifer(null, "Dictionary"), "Dictionary", "dictionary", TokenTypes.visPublic, FunctionType.Function,
                dicArgs, new CTypeRef(null, new CDictionaryType(new CTypeRef(null, Variant))));
            Dictionary.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(null, CToken.Identifer(null, "ExecuteAnywhereAttribute")));

            CArgumentList refArgs = new CArgumentList();
            refArgs.Add(new CArgument(CToken.Keyword(null, "byval"),
                CToken.Identifer(null, "func"),
                new CTypeRef(null, BuiltIns.String)));
            GetRef = new CFunction(CToken.Identifer(null, "GetRef"), "GetRef", "getref", TokenTypes.visPublic, FunctionType.Function, refArgs,
                new CTypeRef(null, BuiltIns.FunctionPlaceHolder));
            GetRef.Attributes.Add(CToken.Identifer(null, "ExecuteAnywhere"), new CTypeRef(null, CToken.Identifer(null, "ExecuteAnywhereAttribute")));

            // add string ienumerator interface
            CToken ifaceName = CToken.Identifer(null, "System.Collections.Generic.IEnumerable`1<char>");
            CInterface iface = new CInterface(ifaceName, ifaceName);
            iface.GenericParameters.Add(Character);
            String.AddInterface(new CTypeRef(null, iface));
        }

        private static void SetupGlobalTypes()
        {
            Byte.IsNumeric = Int32.IsNumeric = Double.IsNumeric = Date.IsNumeric = Int64.IsNumeric = true;
            Character.IsObject = Byte.IsObject = String.IsObject = Int32.IsObject = Boolean.IsObject = Date.IsObject = Double.IsObject = false;

            Object.IsObject = true;

            // Conversion matrix
            //
            //     SIDBFL
            //    S100000
            //    I110011
            //    D101000
            //    B100100
            //    F100010
            //    L100001
            //

            Int32.EnableConversionTo(Double);
            Int32.EnableConversionTo(Int64);

            DbNull.IsObject = false;

            Variant.IsInferable = DbNull.IsInferable = Nothing.IsInferable = false;
        }
    }
}

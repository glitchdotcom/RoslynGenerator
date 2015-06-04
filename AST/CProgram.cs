using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace FogCreek.Wasabi.AST
{
    public class CProgram : CNode, IAttributed
    {
        private static CProgram global;

        public static CProgram Global
        {
            get
            {
                if (global == null)
                    return new CProgram();
                return global;
            }
        }

        private CAttributeList attribs = new CAttributeList();

        private List<CVariable> variables = new List<CVariable>();
        private Dictionary<string, CFunction> clientfunctions = new Dictionary<string, CFunction>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, CFunction> serverfunctions = new Dictionary<string, CFunction>(StringComparer.OrdinalIgnoreCase);
        private SortedList<string, CClass> classes = new SortedList<string, CClass>(StringComparer.OrdinalIgnoreCase);
        private List<CClass[]> duplicateClasses = new List<CClass[]>();
        private List<CFunction> clientfunctionsList = new List<CFunction>();
        private List<CFunction> serverfunctionsList = new List<CFunction>();
        private List<CClass> classesList = new List<CClass>();
        private SortedList<string, CClass> universalClasses = new SortedList<string, CClass>(StringComparer.OrdinalIgnoreCase);
        private SortedList<string, CConst> constants = new SortedList<string, CConst>(StringComparer.OrdinalIgnoreCase);

        private List<Mono.Cecil.AssemblyDefinition> assemblies = new List<AssemblyDefinition>();
        private List<string> assemblyNames = new List<string>();

        private Dictionary<string, CFunction> webEntryPoints = new Dictionary<string, CFunction>();
        private CFunction webInitHandler;
        private CFunction webErrorHandler;
        private CFunction webCleanupHandler;
        private CFunction webDefaultEntryPoint;
        private CFunction webEntryPointRewriter;

        public Dictionary<string, CFunction> WebEntryPoints { get { return webEntryPoints; } }
        private List<CFile> files = new List<CFile>();
        private Dictionary<string, CFile> fileMap = new Dictionary<string, CFile>();

        public List<AssemblyDefinition> Assemblies
        {
            get { return assemblies; }
        }

        public List<string> AssemblyNames
        {
            get { return assemblyNames; }
        }

        public CFunction WebInitHandler
        {
            get { return webInitHandler; }
            set { webInitHandler = value; }
        }

        public CFunction WebErrorHandler
        {
            get { return webErrorHandler; }
            set { webErrorHandler = value; }
        }

        public CFunction WebCleanupHandler
        {
            get { return webCleanupHandler; }
            set { webCleanupHandler = value; }
        }

        public CFunction WebDefaultEntryPoint
        {
            get { return webDefaultEntryPoint; }
            set { webDefaultEntryPoint = value; }
        }

        public CFunction WebEntryPointRewriter
        {
            get { return webEntryPointRewriter; }
            set { webEntryPointRewriter = value; }
        }

        private CProgram()
            : base(null)
        {
            global = this;

            classes["String"] = BuiltIns.String;
            classes["Int32"] = BuiltIns.Int32;
            classes["Int64"] = BuiltIns.Int32;
            classes["Character"] = BuiltIns.Character;
            classes["Boolean"] = BuiltIns.Boolean;
            classes["Date"] = BuiltIns.Date;
            classes["Double"] = BuiltIns.Double;

            classes["__Object"] = BuiltIns.Object;
            classes["__Variant"] = BuiltIns.Variant;

            // lowercase alias
            classes["byte"] = BuiltIns.Byte;
            classes["string"] = BuiltIns.String;
            classes["int32"] = BuiltIns.Int32;
            classes["int64"] = BuiltIns.Int32;
            classes["character"] = BuiltIns.Character;
            classes["boolean"] = BuiltIns.Boolean;
            classes["date"] = BuiltIns.Date;
            classes["double"] = BuiltIns.Double;
            classes["object"] = BuiltIns.Object;
            classes["variant"] = BuiltIns.Variant;

            classes["DbNull"] = BuiltIns.DbNull;
            classes["dbnull"] = BuiltIns.DbNull;
            classes["Nothing"] = BuiltIns.Nothing;

            classes["__Void"] = BuiltIns.Void;

            foreach (KeyValuePair<string, CClass> e in classes)
            {
                e.Value.SetSemanticallyComplete();
                universalClasses[e.Key] = e.Value;
            }

            // Add valueOf method to Date object
            CFunction valueOf = new CFunction(new CToken("", 1, 0, "", TokenTypes.identifier, "valueof", "valueOf", true),
                                              "valueOf", "valueof", TokenTypes.visPublic, FunctionType.Function, new CArgumentList(),
                                              new CTypeRef(null, BuiltIns.Int32));
            valueOf.Attributes.Add(CToken.Identifer(valueOf.Token, "suppressusagewarning"), new CTypeRef(null, CToken.Identifer(valueOf.Token, "SuppressUsageWarningAttribute")));
            valueOf.Attributes.Add(CToken.Identifer(valueOf.Token, "executeonclient"), new CTypeRef(null, CToken.Identifer(valueOf.Token, "ExecuteOnClientAttribute")));
            valueOf.Class = BuiltIns.Date;
            BuiltIns.Date.SetMember("valueof", new CMethod(valueOf));
        }

        CFile dynamicFile;
        public CFile DynamicFile
        {
            get
            {
                if (dynamicFile == null)
                {
                    dynamicFile = new CFile("Dynamic", "Dynamic");
                }
                return dynamicFile;
            }
        }

        private Dictionary<string, CFunction> FunctionsTable
        {
            get
            {
                if (Compiler.Current.CurrentMode == NodeStateMode.Client)
                    return clientfunctions;
                else
                    return serverfunctions;
            }
        }

        public CFunction[] Functions
        {
            get
            {
                if (Compiler.Current.CurrentMode == NodeStateMode.Client)
                    return clientfunctionsList.ToArray();
                else
                    return serverfunctionsList.ToArray();
            }
        }

        public CFunction FindFunction(string name)
        {
            Dictionary<string, CFunction> functions = FunctionsTable;
            CFunction function;
            if (!functions.TryGetValue(name, out function))
                function = null;
            return function;
        }

        public bool TryLookupFunction(string name, out CFunction function)
        {
            Dictionary<string, CFunction> functions = FunctionsTable;
            return functions.TryGetValue(name, out function);
        }

        public CClass[] Classes
        {
            get { return classesList.ToArray(); }
        }

        public SortedList<string, CClass> UniversalClasses
        {
            get { return universalClasses; }
        }

        public SortedList<string, CConst> Constants
        {
            get { return constants; }
        }

        public List<CFile> Files
        {
            get { return files; }
        }

        public CAttributeList Attributes
        {
            get { return attribs; }
        }

        public List<CVariable> Variables
        {
            get { return variables; }
        }

        public bool ClientFunctionExists(string name)
        {
            return clientfunctions.ContainsKey(name);
        }

        public bool ServerFunctionExists(string name)
        {
            return serverfunctions.ContainsKey(name);
        }

        public CFunction[] ClientFunctions
        {
            get { return clientfunctionsList.ToArray(); }
        }

        public CFunction[] ServerFunctions
        {
            get { return serverfunctionsList.ToArray(); }
        }

        public CClass FindClass(String name)
        {
            return FindClass(name, true);
        }

        public CClass FindClass(String name, bool searchImports)
        {
            return InternalFindClass(name, name, searchImports);
        }

        public CClass FindClass(CToken name)
        {
            return FindClass(name, true);
        }

        public CClass FindClass(CToken name, bool searchImports)
        {
            CClass result = null;

            CFile file = null;
            if (name.Filename != null)
                fileMap.TryGetValue(name.Filename, out file);
            
            // first look to see if the type has been renamed
            CToken newName;
            if (file != null && file.ClassNameAliases.TryGetValue(name.RawValue, out newName))
                name = newName;

            // Try to find the class the normal way
            if (result == null)
                result = InternalFindClass(name.RawValue, name.Value, searchImports);
            
            // Look in the namespace mapping
            if (result == null && file != null)
            {
                foreach (var prefix in file.NameSpaceResolutionPrefixes)
                {
                    result = InternalFindClass(
                        prefix.RawValue + "." + name.RawValue,
                        prefix.Value + "." + name.Value,
                        searchImports);
                    if (result != null)
                        break;
                }
            }

            return result;
        }

        private CClass InternalFindClass(string rawname, string name, bool searchImports)
        {
            CClass result;
            result = InternalFindClassByName(name); 
            //if (searchImports && result == null)
            //    if (ClrImporter.ResolveExternalClass(this, rawname))
            //        result = InternalFindClassByName(name);


            if (result == null && !rawname.Contains(".") && Compiler.Current.DefaultNamespaceSet)
            {
                result = InternalFindClass(Compiler.Current.DefaultNamespace.RawValue + "." + rawname,
                    Compiler.Current.DefaultNamespace.Value + "." + name, searchImports);
            }

            return result;
        }

        private CClass InternalFindClassByName(String name)
        {
            CClass result;
            lock (classes)
                if (classes.TryGetValue(name, out result))
                    return result;
            return null;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.VisitProgram(this);
        }

        
        int arrays = 0;
        internal void AddArray(CArrayType array)
        {
            arrays++;
            classes["UNCALLABLE ARRAY " + arrays] = array;
            classesList.Add(array);
        }

        internal void AddClass(string p, CClass type)
        {
            if (classes.ContainsKey(p))
            {
                CClass[] rg = new CClass[] { type, classes[p] };
                duplicateClasses.Add(rg);
            }
            classes[p] = type;
            classesList.Add(type);
        }
    }
}

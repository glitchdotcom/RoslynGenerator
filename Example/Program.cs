using FogCreek.Wasabi;
using FogCreek.Wasabi.AST;
using FogCreek.Wasabi.CodeGenerators;
using System;
using System.IO;
using System.Linq;

namespace Example
{
    class Program
    {
        /// <param name="args">A single optional arg tells the program where to cram its output</param>
        static int Main()
        {
            CProgram.Global.Assemblies.Add(new Mono.Cecil.DefaultAssemblyResolver().Resolve(typeof(object).Assembly.FullName));

            ClrImporter.LoadBuiltins(CProgram.Global);

            var compiler = Compiler.Current = new Compiler
            {
                DefaultNamespaceSet = true,
                DefaultNamespace = new CToken(null, TokenTypes.identifier, "Output"),
                OutputPath = "was_out"
            };

            var rg = new RoslynGenerator();
            IVisitor irg = rg;

            irg.VisitProgram(CProgram.Global);
            irg.VisitFile(FakeFile);

            rg.RootFile("root.cs");
            return 0;
        }

        private const string FILE = "example.was";

        public static CFile FakeFile
        {
            get
            {
                var file = new CFile(FILE, Path.Combine(Directory.GetCurrentDirectory(), FILE));
                file.Statements.Add(MainClass);

                return file;
            }
        }

        public static CClass MainClass
        {
            get
            {
                var @class = new CClass(new CToken(FILE, TokenTypes.identifier, "Example"), "Example");

                @class.SetClassMember("Main", new CMethod(MainFunc));

                @class.SetSemanticallyComplete();

                return @class;
            }
        }

        public static CFunction MainFunc
        {
            get
            {
                var func = new CFunction(new CToken(FILE, TokenTypes.identifier, "Main"),
                    "Main", "Main",
                    TokenTypes.visPrivate,
                    CFunction.vbFunction,
                    new CArgumentList(),
                    BuiltIns.Int32.Type);
                func.IsStatic = true;

                AddBody(func.Statements);

                return func;
            }
        }

        public static void AddBody(CStatementBlock block)
        {
            block.Add(new CComment(new CToken(FILE, TokenTypes.comment, "' This is an example"), " This is an example"));

            block.Add(new CReturn(new CToken(FILE, TokenTypes.keyword, "Return"))
            {
                Expression = new CConstantExpression(new CToken(FILE, TokenTypes.number, "5"))
            });
        }
    }
}

using FogCreek.Wasabi.AST;

namespace FogCreek.Wasabi
{
    public enum CompilerPhase
    {
        Parsing,
        TypeChecking,
        XmlGenerating,
        CodeGenerating,
    }

    public class Compiler
    {
        public static Compiler Current { get; set; }

        public bool DefaultNamespaceSet { get; set; }
        public CToken DefaultNamespace { get; set; }


        public AST.NodeStateMode CurrentMode { get; set; }

        public CompilerPhase CurrentPhase { get; set; }

        public string OutputPath { get; set; }
    }
}

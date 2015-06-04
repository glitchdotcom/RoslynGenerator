using System;
using System.Collections.Generic;
using FogCreek.Wasabi.AST;

namespace FogCreek.Wasabi
{
    internal interface IInterceptor
    {
        void Option(COption option, CodeGenVisitor generator);
        void EnterFunction(CFunction function, CodeGenVisitor generator);
        void ExitFunction(CFunction function, CodeGenVisitor generator);
    }

    internal interface ICodeGenAcceptor : IVisitor
    {
        Object PreAcceptThen(CIf cif, Object state);
        void PreAcceptElse(CIf cif, Object state);
        void PreAcceptEndIf(CIf cif, Object state);
        void EnterFunction(CFunction function);
        void ExitFunction(CFunction function);
    }

    public interface ICodeGenVisitor : IVisitor
    {
        void InstrumentNode(CNode node, int num);
        void PreVisitFile(CFile file);

        IVisitor Acceptor { get; set; }

        void print(string s);
        void println(string s);
        void println();
    }
}
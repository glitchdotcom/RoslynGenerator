# Wasabi RoslynGenerator

This is the very back end of the Wasabi-to-C# compiler from Fog Creek Software.
It converts a Wasabi abstract syntax tree (AST) into C# code.

The CLR importer, lexer, parser, interpreter, type checker, language runtime, JavaScript generator,
and other components of Wasabi are missing.

It is intended to be used as an example for how to write a C# generator using Microsoft Roslyn.

Build the solution in Visual Studio, then run `.\Example\bin\Debug\Example.exe` from the root of the repository.

A very tiny program will be generated in `was_out`.

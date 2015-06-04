using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace FogCreek.Wasabi.CodeGenerators
{
    static class RoslynExtensions
    {
        public static TypeDeclarationSyntax WithBaseList(this TypeDeclarationSyntax node, BaseListSyntax list)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithBaseList(list);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithBaseList(list);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithBaseList(list);
            }
            throw new NotImplementedException("WithBaseList " + node.Kind().ToString());
        }

        public static TypeDeclarationSyntax WithModifiers(this TypeDeclarationSyntax node, SyntaxTokenList modifiers)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithModifiers(modifiers);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithModifiers(modifiers);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithModifiers(modifiers);
            }
            throw new NotImplementedException("WithModifiers " + node.Kind().ToString());
        }

        public static BaseMethodDeclarationSyntax WithModifiers(this BaseMethodDeclarationSyntax node, SyntaxTokenList modifiers)
        {
            switch (node.Kind())
            {
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    throw new NotImplementedException("Wasabi doesn't have operators");
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)node).WithModifiers(modifiers);
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)node).WithModifiers(modifiers);
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)node).WithModifiers(modifiers);
            }
            throw new NotImplementedException("WithModifiers " + node.Kind().ToString());
        }

        public static BasePropertyDeclarationSyntax WithModifiers(this BasePropertyDeclarationSyntax node, SyntaxTokenList modifiers)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)node).WithModifiers(modifiers);
                case SyntaxKind.PropertyDeclaration:
                    return (((PropertyDeclarationSyntax)node)).WithModifiers(modifiers);
            }
            throw new NotImplementedException("WithModifiers " + node.Kind().ToString());
        }

        public static BasePropertyDeclarationSyntax WithExplicitInterfaceSpecifier(this BasePropertyDeclarationSyntax node, ExplicitInterfaceSpecifierSyntax syntax)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)node).WithExplicitInterfaceSpecifier(syntax);
                case SyntaxKind.PropertyDeclaration:
                    return (((PropertyDeclarationSyntax)node)).WithExplicitInterfaceSpecifier(syntax);
            }
            throw new NotImplementedException("WithExplicitInterfaceSpecifier " + node.Kind().ToString());
        }

        public static BasePropertyDeclarationSyntax WithAccessorList(this BasePropertyDeclarationSyntax node, AccessorListSyntax accessorList)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)node).WithAccessorList(accessorList);
                case SyntaxKind.PropertyDeclaration:
                    return (((PropertyDeclarationSyntax)node)).WithAccessorList(accessorList);
            }
            throw new NotImplementedException();
        }

        public static TypeDeclarationSyntax AddMember(this TypeDeclarationSyntax node, MemberDeclarationSyntax member)
        {
            return node.WithMembers(node.Members.Add(member));
        }

        public static TypeDeclarationSyntax WithMembers(this TypeDeclarationSyntax node, SyntaxList<MemberDeclarationSyntax> members)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithMembers(members);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithMembers(members);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithMembers(members);
            }
            throw new NotImplementedException("WithMembers " + node.Kind().ToString());
        }

        public static TypeDeclarationSyntax WithAttributeLists(this TypeDeclarationSyntax node, SyntaxList<AttributeListSyntax> attributes)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithAttributeLists(attributes);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithAttributeLists(attributes);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithAttributeLists(attributes);
            }

            throw new NotImplementedException("WithAttributeLists " + node.Kind().ToString());
        }

        public static BaseMethodDeclarationSyntax WithAttributeLists(this BaseMethodDeclarationSyntax node, SyntaxList<AttributeListSyntax> attributes)
        {
            switch (node.Kind())
            {
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    throw new NotImplementedException("Wasabi doesn't have operators");
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)node).WithAttributeLists(attributes);
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)node).WithAttributeLists(attributes);
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)node).WithAttributeLists(attributes);
            }

            throw new NotImplementedException("WithAttributeLists " + node.Kind().ToString());
        }

        public static BaseMethodDeclarationSyntax WithParameterList(this BaseMethodDeclarationSyntax method, ParameterListSyntax pls)
        {
            switch (method.Kind())
            {
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    throw new NotImplementedException("Wasabi doesn't have operators");
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)method).WithParameterList(pls);
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)method).WithParameterList(pls);
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)method).WithParameterList(pls);
            }

            throw new NotImplementedException("WithParameterList " + method.Kind().ToString());
        }

        public static BaseMethodDeclarationSyntax WithReturnType(this BaseMethodDeclarationSyntax method, TypeSyntax type)
        {
            switch (method.Kind())
            {
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    throw new NotImplementedException("Wasabi doesn't have operators");
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)method).WithReturnType(type);
                case SyntaxKind.ConstructorDeclaration:
                    throw new InvalidOperationException("Constructors don't have return type");
                case SyntaxKind.DestructorDeclaration:
                    throw new InvalidOperationException("Destructors don't have return type");
            }

            throw new NotImplementedException("WithReturnType " + method.Kind().ToString());
        }


        public static BaseMethodDeclarationSyntax WithBody(this BaseMethodDeclarationSyntax method, BlockSyntax body)
        {
            switch (method.Kind())
            {
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    throw new NotImplementedException("Wasabi doesn't have operators");
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)method).WithBody(body);
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)method).WithBody(body);
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)method).WithBody(body);
            }

            throw new NotImplementedException("WithBody " + method.Kind().ToString());
        }
    }
}

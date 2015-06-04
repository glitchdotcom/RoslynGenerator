using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    internal interface IAttributed
    {
        CAttributeList Attributes { get; }
    }
}
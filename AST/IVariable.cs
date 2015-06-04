using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public interface IVariable
    {
        CToken Token { get; }
        String Name { get; }
        CTypeRef Type { get; }
        bool IsArray { get; }
        bool AccessedBeforeUsed { get; }
        int AssignmentCount { get; }
        int AccessCount { get; }

        string RawName { get; }

        void incAssignmentCount(CClass currentclass, CFunction currentfunction);
        void incAccessCount(CClass currentclass, CFunction currentfunction);
        bool canAssign(CClass currentclass, CFunction currentfunction);

        void SetExternallyReferenced();
    }
}
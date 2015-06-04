using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CScope
    {
        public CScope Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        private Dictionary<string, CNode> funcs = new Dictionary<string, CNode>(StringComparer.Ordinal);
        private Dictionary<string, CNode> nodes = new Dictionary<string, CNode>(StringComparer.Ordinal);
        private CScope parent;

        public CScope()
        {
        }

        public CScope(CScope scope)
        {
            foreach (KeyValuePair<string, CNode> pair in scope.funcs)
            {
                funcs[pair.Key] = pair.Value;
            }
            foreach (KeyValuePair<string, CNode> pair in scope.nodes)
            {
                nodes[pair.Key] = pair.Value;
            }
            if (scope.parent != null)
                throw new InvalidOperationException("Cannot clone a parented scope");
        }

        internal void add(String name, CNode node)
        {
            // TODO add previous existance check
            nodes[name] = node;
        }

        public void add(CConst var)
        {
            add(var.Name.Value, var);
        }

        public void add(CVariableBase var)
        {
            add(var.Name.Value, var);
        }

        public void add(CClass type)
        {
            add(type.Name, type);
        }

        public void add(CMember member)
        {
            if (member.MemberType == "field")
                add(member.Name, member.Declared[0]);
            else if (member.MemberType == "const")
                add(member.Name, member.Declared[0]);
            else
                funcs[member.Name] = member;
        }

        public void add(CFunction func)
        {
            if (func.Class == null)
            {
                funcs[func.Name] = func;
                funcs[func.FunctionAlias.ToLower()] = func;
            }
            switch (func.FunctionType)
            {
                case CFunction.vbPropertySet:
                case CFunction.vbSub:
                    break;

                case CFunction.vbPropertyGet:
                case CFunction.vbFunction:
                default:
                    func.scope.add(func.Name, func);
                    break;
            }
        }

        public bool declared(String name)
        {
            if (local(name))
                return true;
            if (parent != null)
                return parent.declared(name);
            return false;
        }

        public bool declaredFunction(String name)
        {
            if (localFunction(name))
                return true;
            if (parent != null)
                return parent.declaredFunction(name);
            return false;
        }

        public bool local(String name)
        {
            return nodes.ContainsKey(name);
        }

        public bool localFunction(String name)
        {
            return funcs.ContainsKey(name);
        }

        public void setNode(String name, CNode node)
        {
            if (nodes.ContainsKey(name))
                nodes[name] = node;
            else
                parent.setNode(name, node);
        }

        public CNode getNode(String name)
        {
            CNode nod;
            if (!nodes.TryGetValue(name, out nod))
                return parent.getNode(name);
            return nod;
        }

        public CNode getFunction(String name)
        {
            CNode val;
            if (!funcs.TryGetValue(name, out val))
                return parent.getFunction(name);
            return val;
        }

        internal void Clear()
        {
            funcs.Clear();
            nodes.Clear();
        }
    }
}

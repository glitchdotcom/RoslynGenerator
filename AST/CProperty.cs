using System;
using System.Collections.Generic;

namespace FogCreek.Wasabi.AST
{
    public class CProperty : CMember
    {
        public const int ixGet = 0;
        public const int ixSet = 1;

        private CFunction m_get;
        private CFunction m_set;

        public CProperty(CToken token, string name)
            : base(token, name, "property", 3, false)
        {
        }

        public CProperty(CProperty property, bool isUnionMember)
            : base(property.Token, property.Name, "property", 3, isUnionMember)
        {
            Declared[ixGet] = m_get = property.m_get;
            Declared[ixSet] = m_set = property.m_set;
        }

        public CFunction GetAccessor
        {
            get { return m_get; }
            set
            {
                if (m_get != null)
                    throw new InvalidCastException();
                m_get = value;
                Declared[ixGet] = value;
            }
        }

        public CFunction SetAccessor
        {
            get { return m_set; }
            set
            {
                if (m_set != null)
                    throw new InvalidCastException();
                m_set = value;
                Declared[ixSet] = value;
            }
        }

        public override TokenTypes Visibility
        {
            get { return (GetAccessor ?? SetAccessor).Visibility; }
        }

        public override CClass DeclaringClass
        {
            get { return (GetAccessor ?? SetAccessor).DeclaringClass; }
        }
    }
}
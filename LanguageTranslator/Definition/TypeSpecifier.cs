using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Definition
{
    enum SimpleTypeKind
    {
        Class, Struct, Interface, TypeParameter, Error, Enum, Other
    }

	abstract class TypeSpecifier
	{
	}

	class SimpleType : TypeSpecifier
	{
		public string Namespace = string.Empty;
		public string TypeName = string.Empty;
        public SimpleTypeKind TypeKind = SimpleTypeKind.Other;

		public override string ToString()
		{
			return "SimpleType " + TypeName;
		}
	}

	class ArrayType : TypeSpecifier
	{
		public SimpleType BaseType = null;

		public override string ToString()
		{
			return string.Format("ArrayType {0}[]", BaseType);
		}
	}

	class GenericType : TypeSpecifier
	{
		public SimpleType OuterType = null;
		public List<TypeSpecifier> InnerType = new List<TypeSpecifier>();

		public override string ToString()
		{
			return string.Format("GenericType {0}<{1}>", OuterType, string.Join(",", InnerType));
		}
	}

	class GenericTypenameType : TypeSpecifier
	{
		public string Name = string.Empty;

		public override string ToString()
		{
			return "GenericTemplateType " + Name;
		}
	}

	class NullableType : TypeSpecifier
	{
		public SimpleType BaseType = null;

		public override string ToString()
		{
			return string.Format("NullableType {0}?", BaseType);
		}
	}
}

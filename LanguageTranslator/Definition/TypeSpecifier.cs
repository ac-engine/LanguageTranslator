using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Definition
{
	abstract class TypeSpecifier
	{
	}

	class SimpleType : TypeSpecifier
	{
		public string Type = string.Empty;

		public override string ToString()
		{
			return "SimpleType " + Type;
		}
	}

	class ArrayType : TypeSpecifier
	{
		public string BaseType = string.Empty;

		public override string ToString()
		{
			return string.Format("ArrayType {0}[]", BaseType);
		}
	}

	class GenericType : TypeSpecifier
	{
		public string OuterType = string.Empty;
		public List<string> InnerType = new List<string>();

		public override string ToString()
		{
			return string.Format("GenericType {0}<{1}>", OuterType, string.Join(",", InnerType));
		}
	}

	class NullableType : TypeSpecifier
	{
		public string BaseType = string.Empty;

		public override string ToString()
		{
			return string.Format("NullableType {0}?", BaseType);
		}
	}
}

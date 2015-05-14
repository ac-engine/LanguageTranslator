using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Definition
{
	class Definitions
	{
		public List<EnumDef> Enums = new List<EnumDef>();
		public List<ClassDef> Classes = new List<ClassDef>();
	}

	class EnumDef
	{
		public string Namespace = string.Empty;
		public string Name = string.Empty;
		public string Brief = string.Empty;
		public List<EnumMemberDef> Members = new List<EnumMemberDef>();

		public bool IsDefinedBySWIG = false;

		public override string ToString()
		{
			return string.Format("EnumDef {0}", Name);
		}
	}

	class EnumMemberDef
	{
		public string Name = string.Empty;
		public string Brief = string.Empty;

		public Expression Value = null;

		public override string ToString()
		{
			return string.Format("EnumMemberDef {0}", Name);
		}

		/// <summary>
		/// パーサー内部処理用
		/// </summary>
		internal Microsoft.CodeAnalysis.CSharp.Syntax.EnumMemberDeclarationSyntax Internal = null;
	}

	class ClassDef
	{
		public string Name = string.Empty;
		public string Brief = string.Empty;
		public List<MethodDef> Methods = new List<MethodDef>();
		public List<PropertyDef> Properties = new List<PropertyDef>();
		public List<FieldDef> Fields = new List<FieldDef>();

		public override string ToString()
		{
			return string.Format("ClassDef {0}", Name);
		}

		public bool IsDefinedBySWIG = false;
	}

	class FieldDef
	{
		public string Name = string.Empty;
		public string Type = string.Empty;
		public string Brief = string.Empty;
		public Expression Initializer = null;

		public override string ToString()
		{
			return string.Format("FieldDef {0}", Name);
		}
	}

	class PropertyDef
	{
		public string Name = string.Empty;
		public string Type = string.Empty;
		public string Brief = string.Empty;
		public AccessorDef Getter = null;
		public AccessorDef Setter = null;

		public override string ToString()
		{
			return string.Format("PropertyDef {0}", Name);
		}
	}

	class AccessorDef
	{
		public List<Statement> Body = new List<Statement>();
	}

	class MethodDef
	{
		public string Name = string.Empty;
		public string ReturnType = string.Empty;
		public string Brief = string.Empty;
		public List<ParameterDef> Parameters = new List<ParameterDef>();
		public List<Statement> Body = new List<Statement>();

		public override string ToString()
		{
			return string.Format("MethodDef {0}", Name);
		}
	}

	class ParameterDef
	{
		public string Type = string.Empty;
		public string Name = string.Empty;
		public string Brief = string.Empty;

		public override string ToString()
		{
			return string.Format("ParameterDef {0}", Name);
		}
	}
}

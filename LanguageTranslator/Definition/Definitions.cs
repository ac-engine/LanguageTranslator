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
        public List<StructDef> Structs = new List<StructDef>();
        public List<InterfaceDef> Interfaces = new List<InterfaceDef>();
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
		public string Namespace = string.Empty;
		public string Name = string.Empty;
		public string Brief = string.Empty;
        public List<TypeSpecifier> BaseTypes = new List<TypeSpecifier>();
        public List<string> TypeParameters = new List<string>();

		public List<MethodDef> Methods = new List<MethodDef>();
		public List<PropertyDef> Properties = new List<PropertyDef>();
		public List<FieldDef> Fields = new List<FieldDef>();
		public List<OperatorDef> Operators = new List<OperatorDef>();

		public override string ToString()
		{
			return string.Format("ClassDef {0}", Name);
		}

		public bool IsDefinedBySWIG = false;
	}

    class StructDef
    {
		public string Namespace = string.Empty;
        public string Name = string.Empty;
        public string Brief = string.Empty;
        public List<string> TypeParameters = new List<string>();

        public List<MethodDef> Methods = new List<MethodDef>();
        public List<PropertyDef> Properties = new List<PropertyDef>();
        public List<FieldDef> Fields = new List<FieldDef>();
		public List<OperatorDef> Operators = new List<OperatorDef>();

		/// <summary>
		/// パーサー内部処理用
		/// </summary>
		internal Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax Internal = null;

        public override string ToString()
        {
            return string.Format("StructDef {0}", Name);
        }
    }

    class InterfaceDef
    {
        public string Namespace = string.Empty;
        public string Name = string.Empty;
        public string Brief = string.Empty;
        public List<string> TypeParameters = new List<string>();

        public List<MethodDef> Methods = new List<MethodDef>();
        public List<PropertyDef> Properties = new List<PropertyDef>();

        public override string ToString()
        {
            return string.Format("InterfaceDef {0}", Name);
        }
    }

	class FieldDef
	{
		public TypeSpecifier Type = null;
		public string Name = string.Empty;
		public Expression Initializer = null;
		public string Brief = string.Empty;

		/// <summary>
		/// パーサー内部処理用
		/// </summary>
		internal Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax Internal = null;

		public override string ToString()
		{
			return string.Format("FieldDef {0}", Name);
		}
	}

	class PropertyDef
	{
		public TypeSpecifier Type = null;
		public string Name = string.Empty;
		public AccessorDef Getter = null;
		public AccessorDef Setter = null;
		public string Brief = string.Empty;

		/// <summary>
		/// パーサー内部処理用
		/// </summary>
		internal Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax Internal = null;

		public override string ToString()
		{
			return string.Format("PropertyDef {0}", Name);
		}
	}

	class AccessorDef
	{
		public Statement Body = null;

		/// <summary>
		/// パーサー内部処理用
		/// </summary>
		internal Microsoft.CodeAnalysis.CSharp.Syntax.AccessorDeclarationSyntax Internal = null;
	}

	class MethodDef
	{
		public TypeSpecifier ReturnType = null;
		public string Name = string.Empty;
		public string Brief = string.Empty;
		public List<ParameterDef> Parameters = new List<ParameterDef>();
		public List<Statement> Body = new List<Statement>();

		/// <summary>
		/// パーサー内部処理用
		/// </summary>
		internal Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax Internal = null;

		public override string ToString()
		{
			return string.Format("MethodDef {0}", Name);
		}
	}

	class ParameterDef
	{
		public TypeSpecifier Type = null;
		public string Name = string.Empty;
		public string Brief = string.Empty;

		public override string ToString()
		{
			return string.Format("ParameterDef {0}", Name);
		}
	}

    class OperatorDef
    {
        public TypeSpecifier ReturnType = null;
        public string Operator = string.Empty;
		public List<ParameterDef> Parameters = new List<ParameterDef>();
		public List<Statement> Body = new List<Statement>();

		public override string ToString()
		{
			return string.Format("ParameterDef {0}", Operator);
		}
	}
}

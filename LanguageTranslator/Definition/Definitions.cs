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

    class TypeParameterDef
    {
        public string Name = string.Empty;
        public bool IsConstraintedAsValueType = false;
        public bool IsConstraintedAsReferenceType = false;
        public List<TypeSpecifier> BaseTypeConstraints = new List<TypeSpecifier>();

        public override string ToString()
        {
            return string.Format("TypeParameterDef {0}", Name);
        }
    }

    abstract class TypeDef
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public List<TypeSpecifier> BaseTypes { get; private set; }
        public List<TypeParameterDef> TypeParameters { get; private set; }
        public List<MethodDef> Methods { get; private set; }
        public List<PropertyDef> Properties { get; private set; }
        public List<FieldDef> Fields { get; private set; }
        public List<OperatorDef> Operators { get; private set; }

        public TypeDef()
        {
            BaseTypes = new List<TypeSpecifier>();
            TypeParameters = new List<TypeParameterDef>();
            Methods = new List<MethodDef>();
            Properties = new List<PropertyDef>();
            Fields = new List<FieldDef>();
            Operators = new List<OperatorDef>();
        }
    }

    class ClassDef : TypeDef
    {
        public string Brief { get; set; }

        public override string ToString()
        {
            return string.Format("ClassDef {0}", Name);
        }

        public bool IsDefinedBySWIG = false;
    }

    class StructDef : TypeDef
    {
        // BaseTypeはダミー

        public string Brief { get; set; }

        /// <summary>
        /// パーサー内部処理用
        /// </summary>
        internal Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax Internal = null;

        public override string ToString()
        {
            return string.Format("StructDef {0}", Name);
        }
    }

    class InterfaceDef : TypeDef
    {
        // Fields, Operatorsはダミー

        public string Brief { get; set; }

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

using System.Collections.Generic;

namespace LanguageTranslator.Definition
{
    class FieldDef
    {
		public SummaryComment Summary = new SummaryComment();

		public AccessLevel AccessLevel = AccessLevel.Private;
        public bool IsStatic = false;

        public TypeSpecifier Type = null;
        public string Name = string.Empty;
        public Expression Initializer = null;
        
		/// <summary>
		/// fixed array専用(無理やり実装)
		/// </summary>
		public string Argument = null;

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
		public SummaryComment Summary = new SummaryComment();

		public AccessLevel AccessLevel = AccessLevel.Private;
        public bool IsStatic = false;

        public TypeSpecifier Type = null;
        public string Name = string.Empty;
        public AccessorDef Getter = null;
        public AccessorDef Setter = null;
        
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
        public AccessLevel AccessLevel = AccessLevel.Private;
        public Statement Body = null;

        /// <summary>
        /// パーサー内部処理用
        /// </summary>
        internal Microsoft.CodeAnalysis.CSharp.Syntax.AccessorDeclarationSyntax Internal = null;
    }

    class MethodDef:
		ITypeParameters
    {
		public SummaryComment Summary = new SummaryComment();

		public AccessLevel AccessLevel = AccessLevel.Private;
        public bool IsStatic = false;
		public bool IsAbstract = false;

        public TypeSpecifier ReturnType = null;
        public string Name = string.Empty;
       
        public List<ParameterDef> Parameters = new List<ParameterDef>();
        public List<Statement> Body = new List<Statement>();

		public List<TypeParameterDef> TypeParameters { get; protected set; }

        /// <summary>
        /// パーサー内部処理用
        /// </summary>
        internal Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax Internal = null;

		public MethodDef()
		{
			TypeParameters = new List<TypeParameterDef>();
		}

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
        public AccessLevel AccessLevel = AccessLevel.Private;
        public TypeSpecifier ReturnType = null;
        public string Operator = string.Empty;
        public List<ParameterDef> Parameters = new List<ParameterDef>();
        public List<Statement> Body = new List<Statement>();

        public override string ToString()
        {
            return string.Format("ParameterDef {0}", Operator);
        }
    }

    class ConstructorDef
    {
		public SummaryComment Summary = new SummaryComment();

		public AccessLevel AccessLevel = AccessLevel.Private;
        public bool IsStatic = false;

        public List<ParameterDef> Parameters = new List<ParameterDef>();
        public ConstructorInitializer Initializer = null;
        public List<Statement> Body = new List<Statement>();

		/// <summary>
		/// パーサー内部処理用
		/// </summary>
		internal Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax Internal = null;

        public override string ToString()
        {
            return string.Format("ConstructorDef");
        }
    }

    class ConstructorInitializer
    {
        public string ThisOrBase = string.Empty;
        public List<Expression> Arguments = new List<Expression>();

		/// <summary>
		/// パーサー内部処理用
		/// </summary>
		internal Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorInitializerSyntax Internal = null;	 
    }

    class DestructorDef
    {
        public List<Statement> Body = new List<Statement>();

		/// <summary>
		/// パーサー内部処理用
		/// </summary>
		internal Microsoft.CodeAnalysis.CSharp.Syntax.DestructorDeclarationSyntax Internal = null;

        public override string ToString()
        {
            return string.Format("DestructorDef");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using LanguageTranslator.Definition;

namespace LanguageTranslator.Parser
{
	class Parser
	{
		Definition.Definitions definitions = null;

		public List<string> TypesNotParsed = new List<string>();

		public List<string> TypesWhosePrivateNotParsed = new List<string>();

		public Definition.Definitions Parse(string[] pathes)
		{
			definitions = new Definition.Definitions();

			List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
			foreach (var path in pathes)
			{
				var tree = CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(path), null, path);
				syntaxTrees.Add(tree);
			}

			var assemblyPath = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location);

			var mscorelib = MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "mscorlib.dll"));

			var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
						"Compilation",
						syntaxTrees: syntaxTrees.ToArray(),
						references: new[] { mscorelib },
						options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
												  Microsoft.CodeAnalysis.OutputKind.ConsoleApplication));

			// 定義のみ取得
			foreach (var tree in syntaxTrees)
			{
				var semanticModel = compilation.GetSemanticModel(tree);

				var decl = semanticModel.GetDeclarationDiagnostics();
				var methodBodies = semanticModel.GetMethodBodyDiagnostics();
				var root = semanticModel.SyntaxTree.GetRoot();

				ParseRoot(root, semanticModel);
			}

			var blockParser = new BlockParser();
			blockParser.Parse(definitions, syntaxTrees, compilation);

			return definitions;
		}

		void ParseRoot(SyntaxNode root, SemanticModel semanticModel)
		{
			var compilationUnitSyntax = root as CompilationUnitSyntax;

			var usings = compilationUnitSyntax.Usings;
			var members = compilationUnitSyntax.Members;

			foreach (var member in members)
			{
				var namespaceSyntax = member as NamespaceDeclarationSyntax;
				ParseNamespace(namespaceSyntax, semanticModel);
			}
		}

		void ParseNamespace(NamespaceDeclarationSyntax namespaceSyntax, SemanticModel semanticModel)
		{
			var members = namespaceSyntax.Members;

			// TODO 正しいnamespaceの処理
			var nameSyntax_I = namespaceSyntax.Name as IdentifierNameSyntax;
			var nameSyntax_Q = namespaceSyntax.Name as QualifiedNameSyntax;

			string namespace_ = string.Empty;
			if (nameSyntax_I != null) namespace_ = nameSyntax_I.Identifier.ValueText;
			if (nameSyntax_Q != null)
			{
				namespace_ = nameSyntax_Q.ToFullString().Trim();
			}

			foreach (var member in members)
			{
				var classSyntax = member as ClassDeclarationSyntax;
				var enumSyntax = member as EnumDeclarationSyntax;
				var structSyntax = member as StructDeclarationSyntax;

				if (enumSyntax != null)
				{
					ParseEnum(namespace_, enumSyntax, semanticModel);
				}
				if (classSyntax != null)
				{
					ParseClass(namespace_, classSyntax, semanticModel);
				}
				if (structSyntax != null)
				{
					ParseStrcut(namespace_, structSyntax, semanticModel);
				}
			}
		}

		void ParseClass(string namespace_, ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
		{
			var classDef = new ClassDef();

			// swig
			classDef.IsDefinedBySWIG = namespace_.Contains("ace.swig");

			classDef.Name = classSyntax.Identifier.ValueText;

			var fullName = namespace_ + "." + classDef.Name;

			if (TypesNotParsed.Contains(fullName))
			{
				return;
			}

			bool isPrivateNotParsed = TypesWhosePrivateNotParsed.Contains(fullName);

			if (classSyntax.TypeParameterList != null)
			{
				foreach (var item in classSyntax.TypeParameterList.Parameters)
				{
					classDef.TypeParameters.Add(item.Identifier.ValueText);
				}
			}

			if (classSyntax.BaseList != null)
			{
				foreach (var item in classSyntax.BaseList.Types)
				{
					classDef.BaseTypes.Add(ParseTypeSpecifier(item.Type, semanticModel));
				}
			}

			Func<SyntaxTokenList, bool> isSkipped = ts => isPrivateNotParsed && ts.Any(t => t.ValueText == "private");
			foreach (var member in classSyntax.Members)
			{
				var methodSyntax = member as MethodDeclarationSyntax;
				var propertySyntax = member as PropertyDeclarationSyntax;
				var fieldSyntax = member as FieldDeclarationSyntax;

				if (methodSyntax != null && !isSkipped(methodSyntax.Modifiers))
				{
					classDef.Methods.Add(ParseMethod(methodSyntax, semanticModel));
				}
				if (propertySyntax != null && !isSkipped(propertySyntax.Modifiers))
				{
					classDef.Properties.Add(ParseProperty(propertySyntax, semanticModel));
				}
				if (fieldSyntax != null && !isSkipped(fieldSyntax.Modifiers))
				{
					classDef.Fields.AddRange(ParseField(fieldSyntax, semanticModel));
				}
			}

			definitions.Classes.Add(classDef);
		}

		private void ParseStrcut(string namespace_, StructDeclarationSyntax structSyntax, SemanticModel semanticModel)
		{
			var structDef = new StructDef();
			structDef.Name = structSyntax.Identifier.ValueText;

			var fullName = namespace_ + "." + structDef.Name;

			if (TypesNotParsed.Contains(fullName))
			{
				return;
			}

			bool isPrivateNotParsed = TypesWhosePrivateNotParsed.Contains(fullName);

			if (structSyntax.TypeParameterList != null)
			{
				foreach (var item in structSyntax.TypeParameterList.Parameters)
				{
					structDef.TypeParameters.Add(item.Identifier.ValueText);
				}
			}

			Func<SyntaxTokenList, bool> isSkipped = ts => isPrivateNotParsed && ts.Any(t => t.ValueText == "private");
			foreach (var member in structSyntax.Members)
			{
				var methodSyntax = member as MethodDeclarationSyntax;
				var propertySyntax = member as PropertyDeclarationSyntax;
				var fieldSyntax = member as FieldDeclarationSyntax;

				if (methodSyntax != null && !isSkipped(methodSyntax.Modifiers))
				{
					structDef.Methods.Add(ParseMethod(methodSyntax, semanticModel));
				}
				if (propertySyntax != null && !isSkipped(propertySyntax.Modifiers))
				{
					structDef.Properties.Add(ParseProperty(propertySyntax, semanticModel));
				}
				if (fieldSyntax != null && !isSkipped(fieldSyntax.Modifiers))
				{
					structDef.Fields.AddRange(ParseField(fieldSyntax, semanticModel));
				}
			}

			definitions.Structs.Add(structDef);
		}

		private FieldDef[] ParseField(FieldDeclarationSyntax fieldSyntax, SemanticModel semanticModel)
		{
			var fieldDef = new List<FieldDef>();
			var type = ParseTypeSpecifier(fieldSyntax.Declaration.Type, semanticModel);

			foreach (var item in fieldSyntax.Declaration.Variables)
			{
				fieldDef.Add(new FieldDef
				{
					Name = item.Identifier.ValueText,
					Type = type,
				});
			}

			return fieldDef.ToArray();
		}

		private PropertyDef ParseProperty(PropertyDeclarationSyntax propertySyntax, SemanticModel semanticModel)
		{
			var propertyDef = new PropertyDef();
			propertyDef.Name = propertySyntax.Identifier.ValueText;
			propertyDef.Type = ParseTypeSpecifier(propertySyntax.Type, semanticModel);

			foreach (var accessor in propertySyntax.AccessorList.Accessors)
			{
				if (accessor.Keyword.Text == "get")
				{
					propertyDef.Getter = new AccessorDef();
				}
				else if (accessor.Keyword.Text == "set")
				{
					propertyDef.Setter = new AccessorDef();
				}
			}

			return propertyDef;
		}

		private MethodDef ParseMethod(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel)
		{
			var methodDef = new MethodDef();
			methodDef.Name = methodSyntax.Identifier.ValueText;
			methodDef.ReturnType = ParseTypeSpecifier(methodSyntax.ReturnType, semanticModel);

			foreach (var parameter in methodSyntax.ParameterList.Parameters)
			{
				methodDef.Parameters.Add(ParseParameter(parameter, semanticModel));
			}

			methodDef.Internal = methodSyntax;

			return methodDef;
		}

		private TypeSpecifier ParseTypeSpecifier(TypeSyntax typeSyntax, SemanticModel semanticModel)
		{
			if (typeSyntax is ArrayTypeSyntax)
			{
				try
				{
					return new ArrayType
					{
						BaseType = (SimpleType)ParseTypeSpecifier(((ArrayTypeSyntax)typeSyntax).ElementType, semanticModel),
					};
				}
				catch (InvalidCastException)
				{
					throw new ParseException("SimpleType以外の配列は使用禁止です。");
				}
			}
			else if (typeSyntax is NullableTypeSyntax)
			{
				try
				{
					return new NullableType
					{
						BaseType = (SimpleType)ParseTypeSpecifier(((NullableTypeSyntax)typeSyntax).ElementType, semanticModel),
					};
				}
				catch (InvalidCastException)
				{
					throw new ParseException("SimpleType以外のnull可能型は使用禁止です。");
				}
			}
			else if (typeSyntax is GenericNameSyntax)
			{
				var g = (GenericNameSyntax)typeSyntax;
				try
				{
					return new GenericType
					{
						OuterType = new SimpleType
						{
							Namespace = semanticModel.GetTypeInfo(typeSyntax).Type.ContainingNamespace.ToString(),
							TypeName = g.Identifier.ValueText,
						},
						InnerType = g.TypeArgumentList.Arguments.Select(x => (SimpleType)ParseTypeSpecifier(x, semanticModel)).ToList(),
					};
				}
				catch (InvalidCastException)
				{
					throw new ParseException("SimpleType以外のジェネリック型は使用禁止です。");
				}
			}
			else
			{
				return new SimpleType
				{
					Namespace = semanticModel.GetTypeInfo(typeSyntax).Type.ContainingNamespace.ToString(),
					TypeName = typeSyntax.GetText().ToString().Trim(),
				};
			}
		}

		private ParameterDef ParseParameter(ParameterSyntax parameter, SemanticModel semanticModel)
		{
			var parameterDef = new ParameterDef();
			parameterDef.Name = parameter.Identifier.ValueText;
			parameterDef.Type = ParseTypeSpecifier(parameter.Type, semanticModel);

			return parameterDef;
		}

		void ParseEnum(string namespace_, EnumDeclarationSyntax enumSyntax, SemanticModel semanticModel)
		{
			var enumDef = new EnumDef();

			// 名称
			enumDef.Name = enumSyntax.Identifier.ValueText;

			// ネームスペース
			enumDef.Namespace = namespace_;

			// swig
			enumDef.IsDefinedBySWIG = namespace_.Contains("ace.swig");

			foreach (var member in enumSyntax.Members)
			{
				var def = ParseEnumMember(member, semanticModel);
				enumDef.Members.Add(def);
			}

			definitions.Enums.Add(enumDef);
		}

		EnumMemberDef ParseEnumMember(EnumMemberDeclarationSyntax syntax, SemanticModel semanticModel)
		{
			EnumMemberDef dst = new EnumMemberDef();

			// 名称
			dst.Name = syntax.Identifier.ValueText;
			dst.Internal = syntax;

			return dst;
		}
	}
}

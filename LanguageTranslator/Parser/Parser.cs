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
		readonly string swig_namespace_keyword = "asd.swig";

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
			var systemlib = MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "System.dll"));
				
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
                        "Compilation",
                        syntaxTrees: syntaxTrees.ToArray(),
                        references: new[] { mscorelib, systemlib },
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

        private void ParseRoot(SyntaxNode root, SemanticModel semanticModel)
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

        private void ParseNamespace(NamespaceDeclarationSyntax namespaceSyntax, SemanticModel semanticModel)
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
                var interfaceSyntax = member as InterfaceDeclarationSyntax;

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
                if (interfaceSyntax != null)
                {
                    ParseInterface(namespace_, interfaceSyntax, semanticModel);
                }
            }
        }

        private void ParseEnum(string namespace_, EnumDeclarationSyntax enumSyntax, SemanticModel semanticModel)
        {
            var enumDef = new EnumDef();

            // 名称
            enumDef.Name = enumSyntax.Identifier.ValueText;

            // ネームスペース
            enumDef.Namespace = namespace_;

            // swig
			enumDef.IsDefinedBySWIG = namespace_.Contains(swig_namespace_keyword);

            foreach (var member in enumSyntax.Members)
            {
                var def = ParseEnumMember(member, semanticModel);
                enumDef.Members.Add(def);
            }

            definitions.Enums.Add(enumDef);
        }

        private EnumMemberDef ParseEnumMember(EnumMemberDeclarationSyntax syntax, SemanticModel semanticModel)
        {
            EnumMemberDef dst = new EnumMemberDef();

            // 名称
            dst.Name = syntax.Identifier.ValueText;
            dst.Internal = syntax;

            return dst;
        }


        private void ParseTypeDeclaration(TypeDef typeDef, TypeDeclarationSyntax typeSyntax, SemanticModel semanticModel)
        {
            typeDef.AccessLevel = ParseAccessLevel(typeSyntax.Modifiers) ?? AccessLevel.Internal;

            #region Generics
            if (typeSyntax.TypeParameterList != null)
            {
                foreach (var item in typeSyntax.TypeParameterList.Parameters)
                {
                    typeDef.TypeParameters.Add(new TypeParameterDef
                    {
                        Name = item.Identifier.ValueText,
                    });
                }
            }

            if (typeSyntax.ConstraintClauses != null)
            {
                foreach (var item in typeSyntax.ConstraintClauses)
                {
                    var def = typeDef.TypeParameters.First(x => x.Name == item.Name.Identifier.ValueText);
                    foreach (var constraint in item.Constraints)
                    {
                        var classOrStruct = constraint as ClassOrStructConstraintSyntax;
                        var type = constraint as TypeConstraintSyntax;

                        if (classOrStruct != null)
                        {
                            def.IsConstraintedAsValueType = classOrStruct.ClassOrStructKeyword.ValueText == "struct";
                            def.IsConstraintedAsReferenceType = classOrStruct.ClassOrStructKeyword.ValueText == "class";
                        }
                        if (type != null)
                        {
                            def.BaseTypeConstraints.Add(ParseTypeSpecifier(type.Type, semanticModel));
                        }
                    }
                }
            } 
            #endregion

            var isPrivateNotParsed = TypesWhosePrivateNotParsed.Contains(typeDef.Namespace + "." + typeDef.Name);

			// 構造体は現状、継承なしとする
			if (typeSyntax.BaseList != null && typeDef.BaseTypes != null)
            {
                foreach (var item in typeSyntax.BaseList.Types)
                {
                    typeDef.BaseTypes.Add(ParseTypeSpecifier(item.Type, semanticModel));
                }
            }

            #region Members
			Func<AccessLevel, bool> isSkipped = level => isPrivateNotParsed;
            foreach (var member in typeSyntax.Members)
            {
                var methodSyntax = member as MethodDeclarationSyntax;
                if (methodSyntax != null)
                {
                    var method = ParseMethod(methodSyntax, semanticModel);
                    if (!isSkipped(method.AccessLevel))
                    {
                        typeDef.Methods.Add(method);
                    }
                }

                var propertySyntax = member as PropertyDeclarationSyntax;
                if (propertySyntax != null)
                {
                    var property = ParseProperty(propertySyntax, semanticModel);
                    if (!isSkipped(property.AccessLevel))
                    {
                        typeDef.Properties.Add(property);
                    }
                }

                var fieldSyntax = member as FieldDeclarationSyntax;
                if (fieldSyntax != null)
                {
                    var field = ParseField(fieldSyntax, semanticModel);
                    if (!isSkipped(field.AccessLevel))
                    {
                        typeDef.Fields.Add(field);
                    }
                }

                var operatorSyntax = member as OperatorDeclarationSyntax;
                if (operatorSyntax != null)
                {
                    var @operator = ParseOperator(operatorSyntax, semanticModel);
                    if (!isSkipped(@operator.AccessLevel))
                    {
                        typeDef.Operators.Add(@operator);
                    }
                }

                var constructorSyntax = member as ConstructorDeclarationSyntax;
                if (constructorSyntax != null)
                {
                    var constructor = ParseConstructor(constructorSyntax, semanticModel);
                    if (!isSkipped(constructor.AccessLevel))
                    {
                        typeDef.Constructors.Add(constructor);
                    }
                }

                var destructorSyntax = member as DestructorDeclarationSyntax;
                if (destructorSyntax != null)
                {
                    typeDef.Destructors.Add(ParseDestructor(destructorSyntax, semanticModel));
                }
            } 
            #endregion
        }

        private static AccessLevel? ParseAccessLevel(SyntaxTokenList modifiers)
        {
            var modifiersText = modifiers.Select(x => x.ValueText);
            if (modifiersText.Contains("protected"))
            {
                if (modifiersText.Contains("internal"))
                {
                    return AccessLevel.ProtectedInternal;
                }
                else
                {
                    return AccessLevel.Protected;
                }
            }
            else if (modifiersText.Contains("public"))
            {
                return AccessLevel.Public;
            }
            else if (modifiersText.Contains("private"))
            {
                return AccessLevel.Private;
            }
            else if (modifiersText.Contains("internal"))
            {
                return AccessLevel.Internal;
            }
            else
            {
                return null;
            }
        }

        private void ParseClass(string namespace_, ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
        {
            var classDef = new ClassDef();

            // swig
			classDef.IsDefinedBySWIG = namespace_.Contains(swig_namespace_keyword);

            classDef.Namespace = namespace_;
            classDef.Name = classSyntax.Identifier.ValueText;

            var fullName = namespace_ + "." + classDef.Name;

            if (TypesNotParsed.Contains(fullName))
            {
                return;
            }

            var partial = definitions.Classes.FirstOrDefault(x => x.Namespace + "." + x.Name == fullName);
            if (partial != null)
            {
                classDef = partial;
            }

            if (classSyntax.Modifiers.Any(x => x.ValueText == "abstract"))
            {
                classDef.IsAbstract = true;
            }

            ParseTypeDeclaration(classDef, classSyntax, semanticModel);

            definitions.Classes.Add(classDef);
        }

        private void ParseStrcut(string namespace_, StructDeclarationSyntax structSyntax, SemanticModel semanticModel)
        {
            var structDef = new StructDef();
            structDef.Internal = structSyntax;

            structDef.Namespace = namespace_;
            structDef.Name = structSyntax.Identifier.ValueText;

            var fullName = namespace_ + "." + structDef.Name;

            {
                var partial = definitions.Structs.FirstOrDefault(x => x.Namespace + "." + x.Name == fullName);
                if (partial != null)
                {
                    structDef = partial;
                }
            }

            if (TypesNotParsed.Contains(fullName))
            {
                return;
            }

            ParseTypeDeclaration(structDef, structSyntax, semanticModel);

            definitions.Structs.Add(structDef);
        }

        private void ParseInterface(string namespace_, InterfaceDeclarationSyntax interfaceSyntax, SemanticModel semanticModel)
        {
            var interfaceDef = new InterfaceDef();
            interfaceDef.Namespace = namespace_;
            interfaceDef.Name = interfaceSyntax.Identifier.ValueText;

            var fullName = interfaceDef.Namespace + "." + interfaceDef.Name;

            if (TypesNotParsed.Contains(fullName))
            {
                return;
            }

            ParseTypeDeclaration(interfaceDef, interfaceSyntax, semanticModel);

            definitions.Interfaces.Add(interfaceDef);
        }


        private FieldDef ParseField(FieldDeclarationSyntax fieldSyntax, SemanticModel semanticModel)
        {
            var fieldDef = new FieldDef();

            if (fieldSyntax.Declaration.Variables.Count != 1)
            {
                var span = fieldSyntax.SyntaxTree.GetLineSpan(fieldSyntax.Declaration.Variables.Span);
                throw new ParseException(string.Format("{0} : 変数の複数同時宣言は禁止です。", span));
            }

            var type = ParseTypeSpecifier(fieldSyntax.Declaration.Type, semanticModel);

            fieldDef.Internal = fieldSyntax;
            fieldDef.Name = fieldSyntax.Declaration.Variables[0].Identifier.ValueText;
            fieldDef.Type = type;
            fieldDef.AccessLevel = ParseAccessLevel(fieldSyntax.Modifiers) ?? AccessLevel.Private;
            fieldDef.IsStatic = fieldSyntax.Modifiers.Any(x => x.ValueText == "static");

            return fieldDef;
        }

        private PropertyDef ParseProperty(PropertyDeclarationSyntax propertySyntax, SemanticModel semanticModel)
        {
            var propertyDef = new PropertyDef();
            propertyDef.Internal = propertySyntax;

            propertyDef.Name = propertySyntax.Identifier.ValueText;
            propertyDef.Type = ParseTypeSpecifier(propertySyntax.Type, semanticModel);
            propertyDef.AccessLevel = ParseAccessLevel(propertySyntax.Modifiers) ?? AccessLevel.Private;
            propertyDef.IsStatic = propertySyntax.Modifiers.Any(x => x.ValueText == "static");

            foreach (var accessor in propertySyntax.AccessorList.Accessors)
            {
                var acc = new AccessorDef();
                acc.Internal = accessor;
                acc.AccessLevel = ParseAccessLevel(accessor.Modifiers) ?? propertyDef.AccessLevel;

                if (accessor.Keyword.Text == "get")
                {
                    propertyDef.Getter = acc;
                }
                else if (accessor.Keyword.Text == "set")
                {
                    propertyDef.Setter = acc;
                }
            }

            return propertyDef;
        }

        private MethodDef ParseMethod(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel)
        {
            var methodDef = new MethodDef();
            methodDef.Internal = methodSyntax;

            methodDef.Name = methodSyntax.Identifier.ValueText;
            methodDef.ReturnType = ParseTypeSpecifier(methodSyntax.ReturnType, semanticModel);
            methodDef.AccessLevel = ParseAccessLevel(methodSyntax.Modifiers) ?? AccessLevel.Private;
            methodDef.IsStatic = methodSyntax.Modifiers.Any(x => x.ValueText == "static");

            foreach (var parameter in methodSyntax.ParameterList.Parameters)
            {
                methodDef.Parameters.Add(ParseParameter(parameter, semanticModel));
            }

            return methodDef;
        }

        private OperatorDef ParseOperator(OperatorDeclarationSyntax operatorSyntax, SemanticModel semanticModel)
        {
            var operatorDef = new OperatorDef();
            operatorDef.Operator = operatorSyntax.OperatorToken.ValueText;
            operatorDef.ReturnType = ParseTypeSpecifier(operatorSyntax.ReturnType, semanticModel);
            operatorDef.AccessLevel = ParseAccessLevel(operatorSyntax.Modifiers) ?? AccessLevel.Private;

            foreach (var item in operatorSyntax.ParameterList.Parameters)
            {
                operatorDef.Parameters.Add(ParseParameter(item, semanticModel));
            }

            return operatorDef;
        }

        private ParameterDef ParseParameter(ParameterSyntax parameter, SemanticModel semanticModel)
        {
            var parameterDef = new ParameterDef();
            parameterDef.Name = parameter.Identifier.ValueText;
            parameterDef.Type = ParseTypeSpecifier(parameter.Type, semanticModel);

            return parameterDef;
        }

        private ConstructorDef ParseConstructor(ConstructorDeclarationSyntax constructorSyntax, SemanticModel semanticModel)
        {
            var constructorDef = new ConstructorDef();

			constructorDef.Internal = constructorSyntax;

			constructorDef.AccessLevel = ParseAccessLevel(constructorSyntax.Modifiers) ?? AccessLevel.Private;
            constructorDef.IsStatic = constructorSyntax.Modifiers.Any(x => x.ValueText == "static");

            if (constructorSyntax.Initializer != null)
            {
                constructorDef.Initializer = new ConstructorInitializer
                {
                    ThisOrBase = constructorSyntax.Initializer.ThisOrBaseKeyword.ValueText,
                };
            }

            foreach (var parameter in constructorSyntax.ParameterList.Parameters)
            {
                constructorDef.Parameters.Add(ParseParameter(parameter, semanticModel));
            }

            return constructorDef;
        }

        private DestructorDef ParseDestructor(DestructorDeclarationSyntax destructorSyntax, SemanticModel semanticModel)
        {
			var def = new DestructorDef();
			def.Internal = destructorSyntax;
			return def;
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
                
                {
					var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
					var symbolInfo = semanticModel.GetSymbolInfo(typeSyntax);
					
                    return new GenericType
                    {
                        OuterType = new SimpleType
                        {
							Namespace = typeInfo.Type.ContainingNamespace.ToString(),
                            TypeName = g.Identifier.ValueText,
                        },
                        InnerType = g.TypeArgumentList.Arguments.Select(x => ParseTypeSpecifier(x, semanticModel)).ToList(),
                    };
                }
            }
            else
            {
                var type = semanticModel.GetTypeInfo(typeSyntax);

                var specifier = new SimpleType
                {
                    Namespace = type.Type.ContainingNamespace.ToString(),
                    TypeName = type.Type.Name,
                };

                switch (type.Type.TypeKind)
                {
                case TypeKind.Class:
                    specifier.TypeKind = SimpleTypeKind.Class;
                    break;
                case TypeKind.Enum:
                    specifier.TypeKind = SimpleTypeKind.Enum;
                    break;
                case TypeKind.Error:
                    specifier.TypeKind = SimpleTypeKind.Error;
                    break;
                case TypeKind.Interface:
                    specifier.TypeKind = SimpleTypeKind.Interface;
                    break;
                case TypeKind.Struct:
                    specifier.TypeKind = SimpleTypeKind.Struct;
                    break;
                case TypeKind.TypeParameter:
                    specifier.TypeKind = SimpleTypeKind.TypeParameter;
					// 基本的にGenericsの型なのでNamespaceは必要ない
					specifier.Namespace = string.Empty;
                    break;
                default:
                    specifier.TypeKind = SimpleTypeKind.Other;
                    break;
                }

                return specifier;
            }
        }
    }
}

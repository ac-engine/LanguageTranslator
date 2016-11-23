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
	public class ParseException : Exception
	{
		public ParseException()
			: base()
		{
		}

		public ParseException(string message)
			: base(message)
		{
		}
	}

	class BlockParser
	{
		Definition.Definitions definitions = null;
		Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation = null;

		public void Parse(Definition.Definitions definitions, List<SyntaxTree> syntaxTrees, Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation)
		{
			this.definitions = definitions;
			this.compilation = compilation;

			foreach (var enumDef in definitions.Enums)
			{
				ParseEnum(enumDef);
			}

			foreach (var classDef in definitions.Classes)
			{
				ParseClass(classDef);
			}

			foreach (var def in definitions.Structs)
			{
				ParseStruct(def);
			}
		}

		void ParseEnum(Definition.EnumDef enumDef)
		{
			// swigの内部は走査しない
			if (enumDef.IsDefinedBySWIG) return;

			foreach (var member in enumDef.Members)
			{
				ParseEnumMember(member);
			}
		}

		void ParseEnumMember(EnumMemberDef def)
		{
			var syntax = def.Internal;
			var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

			if (syntax.EqualsValue == null) return;

			var eqValue = syntax.EqualsValue.Value;

			def.Value = ParseExpression(eqValue, semanticModel);
		}

		void ParseClass(Definition.ClassDef def)
		{
			if (def.IsDefinedBySWIG) return;
			if (def.IsDefinedDefault) return;

			foreach(var field in def.Fields)
			{
				var semanticModel = compilation.GetSemanticModel(field.Internal.SyntaxTree);

				var v = field.Internal.Declaration.Variables[0];
				if (v.Initializer != null && v.Initializer.Value != null)
				{
					field.Initializer = ParseExpression(v.Initializer.Value, semanticModel);
				}
			}

			foreach(var prop in def.Properties)
			{
				var semanticModel = compilation.GetSemanticModel(prop.Internal.SyntaxTree);

				if (prop.Getter != null && prop.Getter.Internal.Body != null)
				{
					prop.Getter.Body = ParseStatement(prop.Getter.Internal.Body, semanticModel);
				}

				if(prop.Setter != null)
				{
					prop.Setter.Body = ParseStatement(prop.Setter.Internal.Body, semanticModel);
				}
			}

			foreach (var method in def.Methods)
			{
				var semanticModel = compilation.GetSemanticModel(method.Internal.SyntaxTree);

				if (method.Internal.Body == null)
				{
					continue;
				}

				var statement = ParseStatement(method.Internal.Body, semanticModel) as BlockStatement;
				method.Body = statement.Statements.ToList();
			}

			foreach(var cst in def.Constructors)
			{
				if (cst.Internal == null) continue;

				var semanticModel = compilation.GetSemanticModel(cst.Internal.SyntaxTree);

				if (cst.Internal.Body == null) continue;

				if(cst.Initializer != null)
				{
					foreach(var arg in cst.Initializer.Internal.ArgumentList.Arguments)
					{
						cst.Initializer.Arguments.Add(ParseExpression(arg.Expression, semanticModel));
					}
				}

				var statement = ParseStatement(cst.Internal.Body, semanticModel) as BlockStatement;
				cst.Body = statement.Statements.ToList();
			}

			foreach(var dst in def.Destructors)
			{
				if (dst.Internal == null) continue;

				var semanticModel = compilation.GetSemanticModel(dst.Internal.SyntaxTree);

				if (dst.Internal.Body == null) continue;

				var statement = ParseStatement(dst.Internal.Body, semanticModel) as BlockStatement;
				dst.Body = statement.Statements.ToList();
			}
		}

		void ParseStruct(Definition.StructDef def)
		{
			if (def.IsDefinedDefault) return;

			foreach (var field in def.Fields)
			{
				var semanticModel = compilation.GetSemanticModel(field.Internal.SyntaxTree);

				var v = field.Internal.Declaration.Variables[0];
				if (v.Initializer != null && v.Initializer.Value != null)
				{
					field.Initializer = ParseExpression(v.Initializer.Value, semanticModel);
				}
			}

			foreach (var prop in def.Properties)
			{
				var semanticModel = compilation.GetSemanticModel(prop.Internal.SyntaxTree);

				if (prop.Getter != null && prop.Getter.Internal.Body != null)
				{
					prop.Getter.Body = ParseStatement(prop.Getter.Internal.Body, semanticModel);
				}

				if (prop.Setter != null)
				{
					prop.Setter.Body = ParseStatement(prop.Setter.Internal.Body, semanticModel);
				}
			}

			foreach (var method in def.Methods)
			{
				var semanticModel = compilation.GetSemanticModel(method.Internal.SyntaxTree);

				if (method.Internal.Body == null)
				{
					continue;
				}

				var statement = ParseStatement(method.Internal.Body, semanticModel) as BlockStatement;
				method.Body = statement.Statements.ToList();
			}

			foreach (var cst in def.Constructors)
			{
				if (cst.Internal == null) continue;

				var semanticModel = compilation.GetSemanticModel(cst.Internal.SyntaxTree);

				if (cst.Internal.Body == null) continue;

				var statement = ParseStatement(cst.Internal.Body, semanticModel) as BlockStatement;
				cst.Body = statement.Statements.ToList();
			}

			foreach (var dst in def.Destructors)
			{
				if (dst.Internal == null) continue;

				var semanticModel = compilation.GetSemanticModel(dst.Internal.SyntaxTree);

				if (dst.Internal.Body == null) continue;

				var statement = ParseStatement(dst.Internal.Body, semanticModel) as BlockStatement;
				dst.Body = statement.Statements.ToList();
			}
		}

		void ParseInterface(Definition.InterfaceDef def)
		{
			foreach (var prop in def.Properties)
			{
				var semanticModel = compilation.GetSemanticModel(prop.Internal.SyntaxTree);

				if (prop.Getter != null && prop.Getter.Internal.Body != null)
				{
					prop.Getter.Body = ParseStatement(prop.Getter.Internal.Body, semanticModel);
				}

				if (prop.Setter != null)
				{
					prop.Setter.Body = ParseStatement(prop.Setter.Internal.Body, semanticModel);
				}
			}

			foreach (var method in def.Methods)
			{
				var semanticModel = compilation.GetSemanticModel(method.Internal.SyntaxTree);

				if (method.Internal.Body == null)
				{
					continue;
				}

				var statement = ParseStatement(method.Internal.Body, semanticModel) as BlockStatement;
				method.Body = statement.Statements.ToList();
			}
		}


		/// <summary>
		/// 式中のメンバアクセス、定数等を解析する。
		/// </summary>
		/// <param name="syntax"></param>
		/// <param name="semanticModel"></param>
		/// <returns></returns>
		Expression ParseExpression(ExpressionSyntax syntax, SemanticModel semanticModel)
		{
			if(syntax == null)
			{
				return null;
			}
		
			var mae = syntax as MemberAccessExpressionSyntax;
			var gns = syntax as GenericNameSyntax;

			var le = syntax as LiteralExpressionSyntax;
			var ie = syntax as InvocationExpressionSyntax;
			var oce = syntax as ObjectCreationExpressionSyntax;
			var ce = syntax as CastExpressionSyntax;
			var thise = syntax as ThisExpressionSyntax;
			var ae = syntax as AssignmentExpressionSyntax;
			var pe = syntax as ParenthesizedExpressionSyntax;

			var ine = syntax as IdentifierNameSyntax;

			var eae = syntax as ElementAccessExpressionSyntax;
			var be = syntax as BinaryExpressionSyntax;
			var preue = syntax as PrefixUnaryExpressionSyntax;
			var poue = syntax as PostfixUnaryExpressionSyntax;
			var basee = syntax as BaseExpressionSyntax;

			var ace = syntax as ArrayCreationExpressionSyntax;
			var sace = syntax as StackAllocArrayCreationExpressionSyntax;

			var iee = syntax as InitializerExpressionSyntax;
			/*
			var coe = syntax as ConditionalExpressionSyntax;
			var sle = syntax as SimpleLambdaExpressionSyntax;
			var ple = syntax as ParenthesizedLambdaExpressionSyntax;
			var oase = syntax as OmittedArraySizeExpressionSyntax;
			var iace = syntax as ImplicitArrayCreationExpressionSyntax;

			var qua = syntax as QualifiedNameSyntax;
			var predf = syntax as PredefinedTypeSyntax;
			*/

			// 自己の型を解析
			TypeInfo? selfTypeInfo = null;
			selfTypeInfo = semanticModel.GetTypeInfo(syntax);
			var selfType = ParseType(syntax, selfTypeInfo, semanticModel);

			if (mae != null)
			{
				MemberAccessExpression exp = new MemberAccessExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				exp.Name = mae.Name.ToString();

				if (mae.Name is GenericNameSyntax)
				{
					var gns_ = mae.Name as GenericNameSyntax;
					exp.Types = gns_.TypeArgumentList.Arguments.Select(_ => ParseType(_, semanticModel)).ToArray();
				}

				TypeInfo? parentType = null;
				if (mae.Expression != null) parentType = semanticModel.GetTypeInfo(mae.Expression);

				// 種類を取得
				var symbol = semanticModel.GetSymbolInfo(mae);
				var methodSymbol = symbol.Symbol as IMethodSymbol;
				var propertySymbol = symbol.Symbol as IPropertySymbol;

				// 親の種類を探索
				List<ClassDef> classDefPs = new List<ClassDef>();

				EnumDef enumDefP = null;
				InterfaceDef interfaceDefP = null;
				StructDef structDefP = null;

				// プロパティである
				if (propertySymbol != null)
				{
					exp.IsProperty = true;
				}

				if (parentType.HasValue && parentType.Value.Type != null)
				{
					if (parentType.Value.Type.TypeKind == TypeKind.Interface)
					{
						var memName = mae.Name.ToString();
						var sym = semanticModel.GetSymbolInfo(mae);
						var name_ = parentType.Value.Type.Name;
						var namespace_ = Utils.ToStr(parentType.Value.Type.ContainingNamespace);
						interfaceDefP = definitions.Interfaces.Where(_ => _.Namespace == namespace_ && _.Name == name_).FirstOrDefault();
					}
					else if (parentType.Value.Type.TypeKind == TypeKind.Class)
					{
						var memName = mae.Name.ToString();
						var sym = semanticModel.GetSymbolInfo(mae);
						var name_ = parentType.Value.Type.Name;
						var namespace_ = Utils.ToStr(parentType.Value.Type.ContainingNamespace);

						classDefPs = definitions.FindTypeWithBases(namespace_, name_).OfType<ClassDef>().ToList();
					}
					else if (parentType.Value.Type.TypeKind == TypeKind.Enum)
					{
						var enumName = selfTypeInfo.Value.Type.Name;
						var namespace_ = Utils.ToStr(selfTypeInfo.Value.Type.ContainingNamespace);
						enumDefP = definitions.Enums.Where(_ => _.Namespace == namespace_ && _.Name == enumName).FirstOrDefault();
					}
					else if (parentType.Value.Type.TypeKind == TypeKind.Struct)
					{
						var memName = mae.Name.ToString();
						var sym = semanticModel.GetSymbolInfo(mae);
						var name_ = parentType.Value.Type.Name;
						var namespace_ = Utils.ToStr(parentType.Value.Type.ContainingNamespace);
						structDefP = definitions.Structs.Where(_ => _.Namespace == namespace_ && _.Name == name_).FirstOrDefault();
					}
				}

				// 親から子を探索
				if (interfaceDefP != null)
				{
					if (methodSymbol != null)
					{
						var method = interfaceDefP.Methods.Where(_ =>
						{
							if (_.Name != methodSymbol.Name) return false;
							if (_.Parameters.Count() != methodSymbol.Parameters.Count()) return false;

							for (int i = 0; i < _.Parameters.Count(); i++)
							{
								if (_.Parameters[i].Name != methodSymbol.Parameters[i].Name) return false;

								// TODO 正しい変換
								//if(_.Parameters[i].Type != methodSymbol.Parameters[i].Type)
							}

							return true;
						}).FirstOrDefault();

						if (method != null)
						{
							exp.Name = null;
							exp.Method = method;
						}
					}
					else if (propertySymbol != null)
					{
						var prop = interfaceDefP.Properties.Where(_ =>
						{
							if (_.Name != propertySymbol.Name) return false;
							return true;
						}).FirstOrDefault();

						if (prop != null)
						{
							exp.Name = null;
							exp.Property = prop;
						}
					}
				}
				else if (classDefPs.Count > 0)
				{
					if (methodSymbol != null)
					{
						foreach (var classDefP in classDefPs)
						{
							var method = classDefP.Methods.Where(_ =>
							{
								if (_.Name != methodSymbol.Name) return false;
								if (_.Parameters.Count() != methodSymbol.Parameters.Count()) return false;

								for (int i = 0; i < _.Parameters.Count(); i++)
								{
									if (_.Parameters[i].Name != methodSymbol.Parameters[i].Name) return false;

									// TODO 正しい変換
									//if(_.Parameters[i].Type != methodSymbol.Parameters[i].Type)
								}

								return true;
							}).FirstOrDefault();

							if (method != null)
							{
								exp.Name = null;
								exp.Class = classDefP;
								exp.Method = method;

								// staticの場合走査停止
								if (method.IsStatic) return exp;
								break;
							}
						}
					}
					else if (propertySymbol != null)
					{
						foreach (var classDefP in classDefPs)
						{
							var prop = classDefP.Properties.Where(_ =>
							{
								if (_.Name != propertySymbol.Name) return false;
								return true;
							}).FirstOrDefault();

							if (prop != null)
							{
								exp.Name = null;
								exp.Class = classDefP;
								exp.Property = prop;
								break;
							}
						}
					}
				}
				else if (structDefP != null)
				{
					if (propertySymbol != null)
					{
						var prop = structDefP.Properties.Where(_ =>
						{
							if (_.Name != propertySymbol.Name) return false;
							return true;
						}).FirstOrDefault();

						if (prop != null)
						{
							exp.Name = null;
							exp.Struct = structDefP;
							exp.Property = prop;
						}
					}
				}
				else if (enumDefP != null)
				{
					var name = mae.Name.ToString();
					exp.EnumMember = enumDefP.Members.Where(_ => _.Name == name).FirstOrDefault();
					if (exp.EnumMember != null)
					{
						exp.Enum = enumDefP;
						exp.Name = null;
					}
				}
				else
				{
					// 代替処理
					if (propertySymbol != null)
					{
						exp.Property = new PropertyDef();
						exp.Property.Name = exp.Name;
					}
				}

				if (exp.EnumMember != null)
				{
					// enumのメンバーだった場合、親は必ずenumなのでこれ以上走査しない
				}
				else if (mae.Expression != null)
				{
					exp.Expression = ParseExpression(mae.Expression, semanticModel);
				}

				return exp;
			}
			else if (gns != null)
			{
				var symbol = semanticModel.GetSymbolInfo(gns);
				var methodSymbol = symbol.Symbol as IMethodSymbol;
				var fieldSymbol = symbol.Symbol as IFieldSymbol;
				var propertySymbol = symbol.Symbol as IPropertySymbol;

				var exp = new GenericNameExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;


				exp.Name = gns.Identifier.ValueText;

				if (methodSymbol != null)
				{
					exp.IsMethod = true;
				}

				if (propertySymbol != null)
				{
					exp.IsProperty = true;
				}

				exp.Types = gns.TypeArgumentList.Arguments.Select(_ => ParseType(_, semanticModel)).ToArray();
				return exp;
			}
			else if (le != null)
			{
				var text = le.GetText().ToString();
				var exp = new LiteralExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;


				exp.Text = text;

				return exp;
			}
			else if (ie != null)
			{
				var exp = new InvocationExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;


				exp.Method = ParseExpression(ie.Expression, semanticModel);
				exp.Args = ie.ArgumentList.Arguments.Select(_ => ParseExpression(_.Expression, semanticModel)).ToArray();

				return exp;
			}
			else if (oce != null)
			{
				var exp = new ObjectCreationExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				exp.Type = ParseType(oce.Type, semanticModel);

				if (oce.ArgumentList != null)
				{
					exp.Args = oce.ArgumentList.Arguments.Select(_ => ParseExpression(_.Expression, semanticModel)).ToArray();
				}
				else
				{
					exp.Args = new Expression[0];
				}

				return exp;
			}
			else if (ce != null)
			{
				var exp = new CastExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				exp.Type = ParseType(ce.Type, semanticModel);
				exp.Expression = ParseExpression(ce.Expression, semanticModel);
				return exp;
			}
			else if (thise != null)
			{
				var exp = new ThisExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				return exp;
			}
			else if (ae != null)
			{
				var exp = new AssignmentExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				if (ae.Kind() == SyntaxKind.AddAssignmentExpression) exp.Type = AssignmentExpression.OperatorType.Add;
				if (ae.Kind() == SyntaxKind.SubtractAssignmentExpression) exp.Type = AssignmentExpression.OperatorType.Substract;
				if (ae.Kind() == SyntaxKind.SimpleAssignmentExpression) exp.Type = AssignmentExpression.OperatorType.Simple;
				if (ae.Kind() == SyntaxKind.DivideAssignmentExpression) exp.Type = AssignmentExpression.OperatorType.Divide;
				if (ae.Kind() == SyntaxKind.ModuloAssignmentExpression) exp.Type = AssignmentExpression.OperatorType.Modulo;

				exp.Temp = ae.Kind();
				exp.Target = ParseExpression(ae.Left, semanticModel);
				exp.Expression = ParseExpression(ae.Right, semanticModel);

				return exp;
			}
			else if (pe != null)
			{
				// ()の構文
				return ParseExpression(pe.Expression, semanticModel);
			}
			else if (ine != null)
			{
				var symbol = semanticModel.GetSymbolInfo(ine);
				var methodSymbol = symbol.Symbol as IMethodSymbol;
				var fieldSymbol = symbol.Symbol as IFieldSymbol;
				var propertySymbol = symbol.Symbol as IPropertySymbol;

				var exp = new IdentifierNameExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				exp.Name = ine.Identifier.Text;

				if (selfTypeInfo?.Type != null)
				{
					exp.Type = ParseType(selfTypeInfo.Value.Type);
				}

				if (methodSymbol != null)
				{
					exp.IsMethod = true;
				}

				if (propertySymbol != null)
				{
					exp.IsProperty = true;
				}

				return exp;
			}
			else if (eae != null)
			{
				if (eae.ArgumentList.Arguments.Count() != 1)
				{
					throw new ParseException("多次元配列は使用禁止です。");
				}

				var value_ = eae.Expression;

				var arg = eae.ArgumentList.Arguments[0].Expression;

				var exp = new ElementAccessExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				exp.Value = ParseExpression(value_, semanticModel);
				exp.Arg = ParseExpression(arg, semanticModel);

				return exp;
			}
			else if (be != null)
			{
				var exp = new BinaryExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				exp.Left = ParseExpression(be.Left, semanticModel);
				exp.Right = ParseExpression(be.Right, semanticModel);

				if (be.Kind() == SyntaxKind.AddExpression) exp.Operator = BinaryExpression.OperatorType.Add;
				if (be.Kind() == SyntaxKind.SubtractExpression) exp.Operator = BinaryExpression.OperatorType.Subtract;
				if (be.Kind() == SyntaxKind.IsExpression) exp.Operator = BinaryExpression.OperatorType.Is;
				if (be.Kind() == SyntaxKind.AsExpression) exp.Operator = BinaryExpression.OperatorType.As;
				if (be.Kind() == SyntaxKind.EqualsExpression) exp.Operator = BinaryExpression.OperatorType.Equals;
				if (be.Kind() == SyntaxKind.NotEqualsExpression) exp.Operator = BinaryExpression.OperatorType.NotEquals;

				if (be.Kind() == SyntaxKind.LogicalAndExpression) exp.Operator = BinaryExpression.OperatorType.LogicalAnd;
				if (be.Kind() == SyntaxKind.LogicalOrExpression) exp.Operator = BinaryExpression.OperatorType.LogicalOr;

				if (be.Kind() == SyntaxKind.GreaterThanExpression) exp.Operator = BinaryExpression.OperatorType.GreaterThan;
				if (be.Kind() == SyntaxKind.GreaterThanOrEqualExpression) exp.Operator = BinaryExpression.OperatorType.GreaterThanOrEqual;

				if (be.Kind() == SyntaxKind.LessThanExpression) exp.Operator = BinaryExpression.OperatorType.LessThan;
				if (be.Kind() == SyntaxKind.LessThanOrEqualExpression) exp.Operator = BinaryExpression.OperatorType.LessThanOrEqual;

				if (be.Kind() == SyntaxKind.MultiplyExpression) exp.Operator = BinaryExpression.OperatorType.Multiply;
				if (be.Kind() == SyntaxKind.DivideExpression) exp.Operator = BinaryExpression.OperatorType.Divide;

				if (be.Kind() == SyntaxKind.ModuloExpression) exp.Operator = BinaryExpression.OperatorType.Modulo;

				if (exp.Operator == BinaryExpression.OperatorType.None)
				{
					var span_ = syntax.SyntaxTree.GetLineSpan(syntax.Span);
					Console.WriteLine(string.Format("{0} : {1} には未対応です。", span_, be.Kind()));
				}

				return exp;
			}
			else if (preue != null)
			{
				var exp = new PrefixUnaryExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				exp.Expression = ParseExpression(preue.Operand, semanticModel);

				switch (preue.Kind())
				{
					case SyntaxKind.LogicalNotExpression:
						exp.Type = PrefixUnaryExpression.OperatorType.LogicalNot;
						break;
					case SyntaxKind.UnaryPlusExpression:
						exp.Type = PrefixUnaryExpression.OperatorType.UnaryPlus;
						break;
					case SyntaxKind.UnaryMinusExpression:
						exp.Type = PrefixUnaryExpression.OperatorType.UnaryMinus;
						break;
					case SyntaxKind.PreIncrementExpression:
						exp.Type = PrefixUnaryExpression.OperatorType.PreIncrement;
						break;
					default:
						throw new Exception();
						break;
				}

				return exp;
			}
			else if (poue != null)
			{
				var exp = new PostfixUnaryExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				exp.Operand = ParseExpression(poue.Operand, semanticModel);

				if (poue.Kind() == SyntaxKind.PostIncrementExpression) exp.Type = PostfixUnaryExpression.OperatorType.PostIncrement;
				if (poue.Kind() == SyntaxKind.PostDecrementExpression) exp.Type = PostfixUnaryExpression.OperatorType.PostDecrement;

				return exp;
			}
			else if (basee != null)
			{
				var exp = new BaseExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				return exp;
			}
			else if (iee != null)
			{
				var exp = new InitializerExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				var expressions = iee.Expressions.Select(_ => _).ToArray();
				exp.Expressions = expressions.Select(_ => ParseExpression(_, semanticModel)).ToArray();

				return exp;
			}
			else if (ace != null || sace != null)
			{
				// stackallocも含め、配列の確保として扱う。

				ArrayTypeSyntax ats = null;
				if (ace != null) ats = ace.Type;
				if (sace != null) ats = sace.Type as ArrayTypeSyntax;

				var exp = new ObjectArrayCreationExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;

				exp.Type = ParseType(ats.ElementType, semanticModel);
				exp.Args = ats.RankSpecifiers.Select(_ => ParseExpression(_.Sizes.FirstOrDefault(), semanticModel)).ToArray();

				return exp;
			}
			else if (syntax is PredefinedTypeSyntax)
			{
				var s = syntax as PredefinedTypeSyntax;
				var exp = new TypeExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;
				return exp;
			}
			else if (syntax is QualifiedNameSyntax)
			{
				var s = syntax as QualifiedNameSyntax;

				var exp = new TypeExpression();
				exp.SelfType = selfType;
				exp.Internal = syntax;
				return exp;
			}

			var span = syntax.SyntaxTree.GetLineSpan(syntax.Span);
			Console.WriteLine(string.Format("{0} : {1} には未対応です。", span, syntax.GetType()));
			return null;
		}

		public Statement ParseStatement(StatementSyntax syntax, SemanticModel semanticModel)
		{
			var bs = syntax as BlockSyntax;
			var ifs = syntax as IfStatementSyntax;
			var fors = syntax as ForStatementSyntax;
			var foreachs = syntax as ForEachStatementSyntax;
			var whiles = syntax as WhileStatementSyntax;
			var continues = syntax as ContinueStatementSyntax;
			var breaks = syntax as BreakStatementSyntax;
			var returns = syntax as ReturnStatementSyntax;
			var locals = syntax as LocalDeclarationStatementSyntax;
			var exs = syntax as ExpressionStatementSyntax;
			var fxs = syntax as FixedStatementSyntax;
			var locks = syntax as LockStatementSyntax;

			var switchs = syntax as SwitchStatementSyntax;
			var trys = syntax as TryStatementSyntax;
			var throws = syntax as ThrowStatementSyntax;

			if (bs != null)
			{
				return ParseBlockStatement(bs, semanticModel);
			}
			else if (ifs != null)
			{
				var st = new IfStatement();
				st.Condition = ParseExpression(ifs.Condition, semanticModel);
				st.TrueStatement = ParseStatement(ifs.Statement, semanticModel);

				if (ifs.Else != null)
				{
					st.FalseStatement = ParseStatement(ifs.Else.Statement, semanticModel);
				}

				return st;
			}
			else if (fors != null)
			{
				var st = new ForStatement();


				st.Condition = ParseExpression(fors.Condition, semanticModel);

				if (fors.Declaration.Variables.Count != 1)
				{
					var span = syntax.SyntaxTree.GetLineSpan(fors.Declaration.Variables.Span);
					throw new ParseException(string.Format("{0} : for内の変数の複数同時宣言は禁止です。", span));
				}

				if (fors.Incrementors.Count >= 2)
				{
					throw new ParseException("for文内の,は使用禁止です。");
				}

				st.Declaration = ParseVariableDeclarationSyntax(fors.Declaration, semanticModel);

				if (fors.Incrementors.Count == 1)
				{
					st.Incrementor = ParseExpression(fors.Incrementors[0], semanticModel);
				}

				st.Statement = ParseStatement(fors.Statement, semanticModel);
				return st;
			}
			else if (foreachs != null)
			{
				var st = new ForeachStatement();

				var type = foreachs.Type;

				st.Type = ParseType(type, semanticModel);
				st.Name = foreachs.Identifier.ValueText;
				st.Value = ParseExpression(foreachs.Expression, semanticModel);
				st.Statement = ParseStatement(foreachs.Statement, semanticModel);

				return st;
			}
			else if (whiles != null)
			{
				var st = new WhileStatement();

				st.Condition = ParseExpression(whiles.Condition, semanticModel);
				st.Statement = ParseStatement(whiles.Statement, semanticModel);

				return st;
			}
			else if (continues != null)
			{
				var st = new ContinueStatement();
				return st;
			}
			else if (breaks != null)
			{
				var st = new BreakStatement();
				return st;
			}
			else if (returns != null)
			{
				var st = new ReturnStatement();

				st.Return = ParseExpression(returns.Expression, semanticModel);

				return st;
			}
			else if (locals != null)
			{
				return ParseLocalDeclaration(locals, semanticModel);
			}
			else if (exs != null)
			{
				var st = new ExpressionStatement();
				st.Expression = ParseExpression(exs.Expression, semanticModel);
				return st;
			}
			else if (fxs != null)
			{
				// fixed構文は配列宣言+ブロックに分解
				var blocks = ParseStatement(fxs.Statement, semanticModel);
				if (!(blocks is BlockStatement)) return null;

				var vs = ParseVariableDeclarationSyntax(fxs.Declaration, semanticModel);
				if (vs == null) return null;

				(blocks as BlockStatement).Statements = (new[] { vs }).Concat((blocks as BlockStatement).Statements).ToArray();

				return blocks;
			}
			else if (locks != null)
			{
				var st = new LockStatement();
				st.Expression = ParseExpression(locks.Expression, semanticModel);
				st.Statement = ParseStatement(locks.Statement, semanticModel);

				return st;
			}
			else if (syntax == null)
			{
				return null;
			}

			{
				var span = syntax.SyntaxTree.GetLineSpan(syntax.Span);
				Console.WriteLine(string.Format("{0} : {1} には未対応です。", span, syntax.GetType()));
			}
			
			return null;
		}

		public VariableDeclarationStatement ParseLocalDeclaration(LocalDeclarationStatementSyntax syntax, SemanticModel semanticModel)
		{
			// const等は無視
			return ParseVariableDeclarationSyntax(syntax.Declaration, semanticModel);
		}

		public VariableDeclarationStatement ParseVariableDeclarationSyntax(VariableDeclarationSyntax syntax, SemanticModel semanticModel)
		{
			if (syntax.Variables.Count != 1)
			{
				var span = syntax.SyntaxTree.GetLineSpan(syntax.Variables.Span);
				throw new ParseException(string.Format("{0} : 変数の複数同時宣言は禁止です。", span));
			}

			var st = new VariableDeclarationStatement();

			var type = syntax.Type;
			var variable = syntax.Variables[0];

			if (variable.Initializer == null ||
				variable.Initializer.Value == null)
			{
				var span = variable.SyntaxTree.GetLineSpan(variable.Span);
				throw new ParseException(string.Format("{0} : {1}, 必ず変数は初期化する必要があります。", span, variable.GetText().ToString()));
			}

			var identifier = variable.Identifier;


			var argumentList = variable.ArgumentList;
			var initializer = variable.Initializer;

			st.Type = ParseType(type, semanticModel);
			st.Name = identifier.ValueText;
			st.Value = ParseExpression(initializer.Value, semanticModel);

			return st;
		}

		public BlockStatement ParseBlockStatement(BlockSyntax syntax, SemanticModel semanticModel)
		{
			// ライン取得
			var blockLineSpan = syntax.SyntaxTree.GetLineSpan(syntax.Span);

			List<Statement> statements = new List<Statement>();

			List<FileLinePositionSpan> statementSpans = new List<FileLinePositionSpan>();

			// 中身
			foreach (var statement in syntax.Statements)
			{
				var statementLineSpan = statement.SyntaxTree.GetLineSpan(statement.Span);

				var result = ParseStatement(statement, semanticModel);
				result.StartingLine = statementLineSpan.StartLinePosition.Line - blockLineSpan.StartLinePosition.Line;
				result.EndingLine = statementLineSpan.EndLinePosition.Line - blockLineSpan.StartLinePosition.Line;

				statementSpans.Add(statementLineSpan);
				statements.Add(result);
			}

			// コメント
			foreach (var t in syntax.DescendantTrivia())
			{
				if (t.IsKind(SyntaxKind.SingleLineCommentTrivia))
				{
					var commentLineSpan = t.SyntaxTree.GetLineSpan(t.Span);

					// ステートメント内は含まない
					if(statementSpans.Any(_=>_.StartLinePosition.Line <= commentLineSpan.StartLinePosition.Line && commentLineSpan.EndLinePosition.Line <= _.EndLinePosition.Line))
					{
						continue;
					}

					var result = new CommentStatement();
					result.Text = t.ToString();
					result.Text = result.Text.Substring(2).Trim();
					result.StartingLine = commentLineSpan.StartLinePosition.Line - blockLineSpan.StartLinePosition.Line;
					result.EndingLine = commentLineSpan.EndLinePosition.Line - blockLineSpan.StartLinePosition.Line;

					statements.Add(result);
				}
			}


			statements = statements.OrderBy(_ => _.StartingLine).ToList();

			var bs = new BlockStatement();
			bs.Statements = statements.ToArray();

			return bs;
		}

		public TypeSpecifier ParseType(TypeSyntax syntax, SemanticModel semanticModel)
		{
			TypeInfo? typeInfo = null;
			typeInfo = semanticModel.GetTypeInfo(syntax);
			return ParseType(syntax, typeInfo, semanticModel);
		}

		private static TypeSpecifier ParseType(ExpressionSyntax syntax, TypeInfo? typeInfo, SemanticModel semanticModel)
		{
			if (typeInfo == null) return null;
			if (!typeInfo.HasValue) return null;

			ITypeSymbol type = null;
			if (typeInfo.Value.Type != null)
			{
				type = typeInfo.Value.Type;
			}
			else
			{
				var symbolInfo = semanticModel.GetSymbolInfo(syntax);
				if (symbolInfo.Symbol != null)
				{
					type = symbolInfo.Symbol as ITypeSymbol;
				}

				if (type == null) return null;
			}

			return ParseType(type);
		}

		private static TypeSpecifier ParseType(ITypeSymbol type)
		{
			var tType = type as ITypeParameterSymbol;
			var namedType = type as INamedTypeSymbol;
			var arrayType = type as IArrayTypeSymbol;
			var pointerType = type as IPointerTypeSymbol;
			var isGeneric = namedType != null && namedType.IsGenericType;

			if (isGeneric)
			{
				var ret = new GenericType();

				var name_ = type.Name;
				var namespace_ = Utils.ToStr(type.ContainingNamespace);
				ret.OuterType = new SimpleType
				{
					Namespace = namespace_,
					TypeName = name_,
				};

				ret.InnerType = namedType.TypeArguments.Select(_ => ParseType(_)).Where(_ => _ != null).ToList();

				if (ret.InnerType.Count() != namedType.TypeArguments.Count())
				{
					throw new Exception();
				}

				return ret;
			}
			else if (type.TypeKind == TypeKind.Array)
			{
				var ret = new ArrayType();
				var name_ = arrayType.ElementType.Name;
				var namespace_ = Utils.ToStr(arrayType.ElementType.ContainingNamespace);
				ret.BaseType = new SimpleType
				{
					Namespace = namespace_,
					TypeName = name_,
				};

				return ret;
			}
			else if (type.TypeKind == TypeKind.Pointer)
			{
				// ポインタは配列にする。
				var ret = new ArrayType();
				var name_ = pointerType.PointedAtType.Name;
				var namespace_ = Utils.ToStr(pointerType.PointedAtType.ContainingNamespace);
				ret.BaseType = new SimpleType
				{
					Namespace = namespace_,
					TypeName = name_,
				};

				return ret;
			}
			else if (type.TypeKind == TypeKind.TypeParameter)
			{
				var name_ = type.Name;
				var ret = new GenericTypenameType
				{
					Name = name_,
				};

				return ret;
			}
			else
			{
				var name_ = type.Name;
				var namespace_ = Utils.ToStr(type.ContainingNamespace);
				var ret = new SimpleType
				{
					Namespace = namespace_,
					TypeName = name_,
				};

				switch (type.TypeKind)
				{
					case TypeKind.Class:
						ret.TypeKind = SimpleTypeKind.Class;
						break;
					case TypeKind.Enum:
						ret.TypeKind = SimpleTypeKind.Enum;
						break;
					case TypeKind.Error:
						ret.TypeKind = SimpleTypeKind.Error;
						break;
					case TypeKind.Interface:
						ret.TypeKind = SimpleTypeKind.Interface;
						break;
					case TypeKind.Struct:
						ret.TypeKind = SimpleTypeKind.Struct;
						break;
					case TypeKind.TypeParameter:
						ret.TypeKind = SimpleTypeKind.TypeParameter;
						// 基本的にGenericsの型なのでNamespaceは必要ない
						ret.Namespace = string.Empty;
						break;
					default:
						ret.TypeKind = SimpleTypeKind.Other;
						break;
				}

				return ret;
			}
		}
	}
}

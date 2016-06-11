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

				method.Body = method.Internal.Body.Statements.Select(_ => ParseStatement(_, semanticModel)).ToList();
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

				cst.Body = cst.Internal.Body.Statements.Select(_ => ParseStatement(_, semanticModel)).ToList();
			}

			foreach(var dst in def.Destructors)
			{
				if (dst.Internal == null) continue;

				var semanticModel = compilation.GetSemanticModel(dst.Internal.SyntaxTree);

				if (dst.Internal.Body == null) continue;

				dst.Body = dst.Internal.Body.Statements.Select(_ => ParseStatement(_, semanticModel)).ToList();
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

				method.Body = method.Internal.Body.Statements.Select(_ => ParseStatement(_, semanticModel)).ToList();
			}

			foreach (var cst in def.Constructors)
			{
				if (cst.Internal == null) continue;

				var semanticModel = compilation.GetSemanticModel(cst.Internal.SyntaxTree);

				if (cst.Internal.Body == null) continue;



				cst.Body = cst.Internal.Body.Statements.Select(_ => ParseStatement(_, semanticModel)).ToList();
			}

			foreach (var dst in def.Destructors)
			{
				if (dst.Internal == null) continue;

				var semanticModel = compilation.GetSemanticModel(dst.Internal.SyntaxTree);

				if (dst.Internal.Body == null) continue;

				dst.Body = dst.Internal.Body.Statements.Select(_ => ParseStatement(_, semanticModel)).ToList();
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

				method.Body = method.Internal.Body.Statements.Select(_ => ParseStatement(_, semanticModel)).ToList();
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
			/*
			var coe = syntax as ConditionalExpressionSyntax;
			var sle = syntax as SimpleLambdaExpressionSyntax;
			var ple = syntax as ParenthesizedLambdaExpressionSyntax;
			var oase = syntax as OmittedArraySizeExpressionSyntax;
			var iace = syntax as ImplicitArrayCreationExpressionSyntax;

			var qua = syntax as QualifiedNameSyntax;
			var predf = syntax as PredefinedTypeSyntax;
			*/

			if (mae != null)
			{
				MemberAccessExpression exp = new MemberAccessExpression();

				exp.Name = mae.Name.ToString();

				if(mae.Name is GenericNameSyntax)
				{
					var gns_ = mae.Name as GenericNameSyntax;
					exp.Types = gns_.TypeArgumentList.Arguments.Select(_ => ParseType(_, semanticModel)).ToArray();
				}

				TypeInfo? selfType = null;
				selfType = semanticModel.GetTypeInfo(mae);

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

				if (parentType.HasValue && parentType.Value.Type != null)
				{
					if (parentType.Value.Type.TypeKind == TypeKind.Interface)
					{
						var memName = mae.Name.ToString();
						var sym = semanticModel.GetSymbolInfo(mae);
						var name_ = parentType.Value.Type.Name;
						var namespace_ = parentType.Value.Type.ContainingNamespace.ToString();
						interfaceDefP = definitions.Interfaces.Where(_ => _.Namespace == namespace_ && _.Name == name_).FirstOrDefault();
					}
					else if(parentType.Value.Type.TypeKind == TypeKind.Class)
					{
						var memName = mae.Name.ToString();
						var sym = semanticModel.GetSymbolInfo(mae);
						var name_ = parentType.Value.Type.Name;
						var namespace_ = parentType.Value.Type.ContainingNamespace.ToString();
						var def = definitions.Classes.Where(_ => _.Namespace == namespace_ && _.Name == name_).FirstOrDefault();

						Action<ClassDef> findBase = null;
						findBase = (c) =>
							{
								if (c != null)
								{
									int count = classDefPs.Count;

									foreach (var t in c.BaseTypes)
									{
										var t_ = definitions.Find(t);
										if (t_ != null && t_ is ClassDef)
										{
											classDefPs.Add(t_ as ClassDef);
										}
									}

									int count_ = classDefPs.Count;

									foreach(var t in classDefPs.Skip(count).Take(count_ - count_).ToArray())
									{
										findBase(t);
									}
								}
							};

						if(def != null)
						{
							classDefPs.Add(def);
							findBase(def);
						}
					}
					else if (parentType.Value.Type.TypeKind == TypeKind.Enum)
					{
						var enumName = selfType.Value.Type.Name;
						var namespace_ = selfType.Value.Type.ContainingNamespace.ToString();
						enumDefP = definitions.Enums.Where(_ => _.Namespace == namespace_ && _.Name == enumName).FirstOrDefault();
					}
					else if (parentType.Value.Type.TypeKind == TypeKind.Struct)
					{
						var memName = mae.Name.ToString();
						var sym = semanticModel.GetSymbolInfo(mae);
						var name_ = parentType.Value.Type.Name;
						var namespace_ = parentType.Value.Type.ContainingNamespace.ToString();
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

				if(exp.EnumMember != null)
				{
					// enumのメンバーだった場合、親は必ずenumなのでこれ以上走査しない
				}
				else if (mae.Expression != null)
				{
					exp.Expression = ParseExpression(mae.Expression, semanticModel);
				}

				return exp;
			}
			else if(gns != null)
			{
				var symbol = semanticModel.GetSymbolInfo(gns);
				var methodSymbol = symbol.Symbol as IMethodSymbol;
				var fieldSymbol = symbol.Symbol as IFieldSymbol;
				var propertySymbol = symbol.Symbol as IPropertySymbol;

				var exp = new GenericNameExpression();
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
				exp.Text = text;

				return exp;
			}
			else if (ie != null)
			{
				var st = new InvocationExpression();

				st.Method = ParseExpression(ie.Expression, semanticModel);
				st.Args = ie.ArgumentList.Arguments.Select(_ => ParseExpression(_.Expression, semanticModel)).ToArray();

				return st;
			}
			else if (oce != null)
			{
				var st = new ObjectCreationExpression();
				st.Type = ParseType(oce.Type, semanticModel);
				st.Args = oce.ArgumentList.Arguments.Select(_ => ParseExpression(_.Expression, semanticModel)).ToArray();

				return st;
			}
			else if (ce != null)
			{
				var st = new CastExpression();

				st.Type = ParseType(ce.Type, semanticModel);
				st.Expression = ParseExpression(ce.Expression, semanticModel);
				return st;
			}
			else if (thise != null)
			{
				var st = new ThisExpression();
				return st;
			}
			else if (ae != null)
			{
				var st = new AssignmentExpression();

				if (ae.Kind() == SyntaxKind.AddAssignmentExpression) st.Type = AssignmentExpression.OperatorType.Add;
				if (ae.Kind() == SyntaxKind.SubtractAssignmentExpression) st.Type = AssignmentExpression.OperatorType.Substract;
				if (ae.Kind() == SyntaxKind.SimpleAssignmentExpression) st.Type = AssignmentExpression.OperatorType.Simple;
				if (ae.Kind() == SyntaxKind.DivideAssignmentExpression) st.Type = AssignmentExpression.OperatorType.Divide;

				st.Temp = ae.Kind();
				st.Target = ParseExpression(ae.Left, semanticModel);
				st.Expression = ParseExpression(ae.Right, semanticModel);

				return st;
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

				var st = new IdentifierNameExpression();
				st.Name = ine.Identifier.Text;

				TypeInfo? selfType = null;
				selfType = semanticModel.GetTypeInfo(ine);

				if (selfType != null && selfType.HasValue && selfType.Value.Type != null)
				{
					st.Type = ParseType(selfType.Value.Type);
				}

				if (methodSymbol != null)
				{
					st.IsMethod = true;
				}

				if (propertySymbol != null)
				{
					st.IsProperty = true;
				}

				return st;
			}
			else if (eae != null)
			{
				if (eae.ArgumentList.Arguments.Count() != 1)
				{
					throw new ParseException("多次元配列は使用禁止です。");
				}

				var exp = eae.Expression;
				var arg = eae.ArgumentList.Arguments[0].Expression;

				var st = new ElementAccessExpression();

				st.Value = ParseExpression(exp, semanticModel);
				st.Arg = ParseExpression(arg, semanticModel);

				return st;
			}
			else if (be != null)
			{
				var st = new BinaryExpression();

				st.Left = ParseExpression(be.Left, semanticModel);
				st.Right = ParseExpression(be.Right, semanticModel);

				if (be.Kind() == SyntaxKind.AddExpression) st.Operator = BinaryExpression.OperatorType.Add;
				if (be.Kind() == SyntaxKind.SubtractExpression) st.Operator = BinaryExpression.OperatorType.Subtract;
				if (be.Kind() == SyntaxKind.IsExpression) st.Operator = BinaryExpression.OperatorType.Is;
				if (be.Kind() == SyntaxKind.AsExpression) st.Operator = BinaryExpression.OperatorType.As;
				if (be.Kind() == SyntaxKind.EqualsExpression) st.Operator = BinaryExpression.OperatorType.Equals;
				if (be.Kind() == SyntaxKind.NotEqualsExpression) st.Operator = BinaryExpression.OperatorType.NotEquals;

				if (be.Kind() == SyntaxKind.LogicalAndExpression) st.Operator = BinaryExpression.OperatorType.LogicalAnd;
				if (be.Kind() == SyntaxKind.LogicalOrExpression) st.Operator = BinaryExpression.OperatorType.LogicalOr;

				if (be.Kind() == SyntaxKind.GreaterThanExpression) st.Operator = BinaryExpression.OperatorType.GreaterThan;
				if (be.Kind() == SyntaxKind.GreaterThanOrEqualExpression) st.Operator = BinaryExpression.OperatorType.GreaterThanOrEqual;

				if (be.Kind() == SyntaxKind.LessThanExpression) st.Operator = BinaryExpression.OperatorType.LessThan;
				if (be.Kind() == SyntaxKind.LessThanOrEqualExpression) st.Operator = BinaryExpression.OperatorType.LessThanOrEqual;

				if (be.Kind() == SyntaxKind.MultiplyExpression) st.Operator = BinaryExpression.OperatorType.Multiply;
				if (be.Kind() == SyntaxKind.DivideExpression) st.Operator = BinaryExpression.OperatorType.Divide;

				return st;
			}
			else if (preue != null)
			{
				var st = new PrefixUnaryExpression();

				st.Expression = ParseExpression(preue.Operand, semanticModel);

				switch(preue.Kind())
				{
					case SyntaxKind.LogicalNotExpression:
						st.Type = PrefixUnaryExpression.OperatorType.LogicalNot;
						break;
					case SyntaxKind.UnaryPlusExpression:
						st.Type = PrefixUnaryExpression.OperatorType.UnaryPlus;
						break;
					case SyntaxKind.UnaryMinusExpression:
						st.Type = PrefixUnaryExpression.OperatorType.UnaryMinus;
						break;
					default:
						throw new Exception();
						break;
				}

				return st;
			}
			else if (poue != null)
			{
				var st = new PostfixUnaryExpression();

				st.Operand = ParseExpression(poue.Operand, semanticModel);

				if (poue.Kind() == SyntaxKind.PostIncrementExpression) st.Type = PostfixUnaryExpression.OperatorType.PostIncrement;
				if (poue.Kind() == SyntaxKind.PostDecrementExpression) st.Type = PostfixUnaryExpression.OperatorType.PostDecrement;

				return st;
			}
			else if(basee != null)
			{
				var st = new BaseExpression();
				return st;
			}
			else if (ace != null || sace != null)
			{
				// stackallocも含め、配列の確保として扱う。

				ArrayTypeSyntax ats = null;
				if (ace != null) ats = ace.Type;
				if (sace != null) ats = sace.Type as ArrayTypeSyntax;

				var st = new ObjectArrayCreationExpression();
				st.Type = ParseType(ats.ElementType, semanticModel);
				st.Args = ats.RankSpecifiers.Select(_ => ParseExpression(_.Sizes.FirstOrDefault(), semanticModel)).ToArray();

				return st;
			}
			else if (syntax is PredefinedTypeSyntax)
			{
				var s = syntax as PredefinedTypeSyntax;
				TypeInfo? selfType = null;
				selfType = semanticModel.GetTypeInfo(syntax);
				var type = ParseType(syntax, selfType, semanticModel);

				var te = new TypeExpression();
				te.Type = type;
				return te;
			}
			else if (syntax is QualifiedNameSyntax)
			{
				var s = syntax as QualifiedNameSyntax;
				TypeInfo? selfType = null;
				selfType = semanticModel.GetTypeInfo(syntax);
				var type = ParseType(syntax, selfType, semanticModel);

				var te = new TypeExpression();
				te.Type = type;
				return te;
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
			List<Statement> statements = new List<Statement>();

			foreach (var statement in syntax.Statements)
			{
				statements.Add(ParseStatement(statement, semanticModel));
			}

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
				var namespace_ = type.ContainingNamespace.ToString();
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
				var namespace_ = arrayType.ElementType.ContainingNamespace.ToString();
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
				var namespace_ = pointerType.PointedAtType.ContainingNamespace.ToString();
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
				var namespace_ = type.ContainingNamespace.ToString();
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

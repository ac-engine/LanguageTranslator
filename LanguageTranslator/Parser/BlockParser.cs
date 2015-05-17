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

			foreach(var enumDef in definitions.Enums)
			{
				ParseEnum(enumDef);
			}
		}

		void ParseEnum(Definition.EnumDef enumDef)
		{
			// swigの内部は走査しない
			if (enumDef.IsDefinedBySWIG) return;

			foreach(var member in enumDef.Members)
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

		/// <summary>
		/// 式中のメンバアクセス、定数等を解析する。
		/// </summary>
		/// <param name="syntax"></param>
		/// <param name="semanticModel"></param>
		/// <returns></returns>
		Expression ParseExpression(ExpressionSyntax syntax, SemanticModel semanticModel)
		{
			var mae = syntax as MemberAccessExpressionSyntax;
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
			var pue = syntax as PrefixUnaryExpressionSyntax;

			if (mae != null)
			{
				MemberAccessExpression exp = new MemberAccessExpression();

				TypeInfo? selfType = null;
				selfType = semanticModel.GetTypeInfo(mae);

				TypeInfo? parentType = null;
				if (mae.Expression != null) parentType = semanticModel.GetTypeInfo(mae.Expression);

				// 親の種類を探索
				EnumDef enumDefP = null;

				if (parentType.HasValue && parentType.Value.Type != null)
				{
					if (parentType.Value.Type.TypeKind == TypeKind.Enum)
					{
						var enumName = selfType.Value.Type.Name;
						var namespace_ = selfType.Value.Type.ContainingNamespace.ToString();
						enumDefP = definitions.Enums.Where(_ => _.Namespace == namespace_ && _.Name == enumName).FirstOrDefault();
					}
				}

				// 親から子を探索

				if (enumDefP != null)
				{
					var name = mae.Name.ToString();
					exp.EnumMember = enumDefP.Members.Where(_ => _.Name == name).FirstOrDefault();
				}
				else
				{
					if (selfType.HasValue && selfType.Value.Type != null)
					{
						if (selfType.Value.Type.TypeKind == TypeKind.Enum)
						{
							var enumName = selfType.Value.Type.Name;
							var namespace_ = selfType.Value.Type.ContainingNamespace.ToString();
							exp.Enum = definitions.Enums.Where(_ => _.Namespace == namespace_ && _.Name == enumName).FirstOrDefault();
						}
					}
				}

				if (mae.Expression != null)
				{
					exp.Expression = ParseExpression(mae.Expression, semanticModel);
				}

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
				st.Type = oce.Type;
				st.Args = oce.ArgumentList.Arguments.Select(_ => ParseExpression(_.Expression, semanticModel)).ToArray();

				return st;
			}
			else if(ce != null)
			{
				var st = new CastExpression();

				st.Type = ce.Type;
				st.Expression = ParseExpression(ce.Expression, semanticModel);
				return st;
			}
			else if(thise != null)
			{
				var st = new ThisExpression();
				return st;
			}
			else if(ae != null)
			{
				var st = new AssignmentExpression();
		
				st.Target = ParseExpression(ae.Left, semanticModel);
				st.Expression = ParseExpression(ae.Right, semanticModel);

				return st;
			}
			else if(pe != null)
			{
				// ()の構文
				return ParseExpression(pe.Expression, semanticModel);
			}
			else if(ine != null)
			{
				var st = new IdentifierNameExpression();
				st.Name = ine.GetText().ToString();
				return st;
			}
			else if(eae != null)
			{
				if(eae.ArgumentList.Arguments.Count() != 1)
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

				return st;
			}
			else if (pue != null)
			{
				var st = new PrefixUnaryExpression();

				st.Expression = ParseExpression(pue.Operand, semanticModel);

				if (pue.Kind() == SyntaxKind.PlusPlusToken) st.Type = PrefixUnaryExpression.OperatorType.PlusPlus;
				if (pue.Kind() == SyntaxKind.MinusMinusToken) st.Type = PrefixUnaryExpression.OperatorType.MinusMinus;

				return st;
			}

			return null;
		}

		public Statement ParseStatement(StatementSyntax syntax, SemanticModel semanticModel)
		{
			var bs = syntax as BlockSyntax;
			var ifs = syntax as IfStatementSyntax;
			var fors = syntax as ForStatementSyntax;
			var foreachs = syntax as ForEachStatementSyntax;
			var continues = syntax as ContinueStatementSyntax;
			var returns = syntax as ReturnStatementSyntax;
			var locals = syntax as LocalDeclarationStatementSyntax;
			var exs = syntax as ExpressionStatementSyntax;

			if(bs != null)
			{
				return ParseBlockStatement(bs, semanticModel);
			}
			else if(ifs != null)
			{
				var st = new IfStatement();
				st.Condition = ParseExpression(ifs.Condition, semanticModel);
				st.TrueStatement = ParseStatement(ifs.Statement, semanticModel);

				if(ifs.Else != null)
				{
					st.FalseStatement = ParseStatement(ifs.Else.Statement, semanticModel);
				}

				return st;
			}
			else if (fors != null)
			{
				var st = new ForStatement();

				st.Condition = ParseExpression(fors.Condition, semanticModel);

				if(fors.Incrementors.Count >= 2)
				{
					throw new ParseException("for文内の,は使用禁止です。");
				}

				// TODO
				// 変数処理(大幅に機能制限する)

				if(fors.Incrementors.Count == 1)
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

				st.Type = type;
				st.Name = foreachs.Identifier.ValueText;
				st.Value = ParseExpression(foreachs.Expression, semanticModel);
				st.Statement = ParseStatement(foreachs.Statement, semanticModel);

				return st;
			}
			else if(continues != null)
			{
				var st = new ContinueStatement();
				return st;
			}
			else if(returns != null)
			{
				var st = new ReturnStatement();

				st.Return = ParseExpression(returns.Expression, semanticModel);

				return st;
			}
			else if(locals != null)
			{
				return ParseLocalDeclaration(locals, semanticModel);
			}
			else if(exs != null)
			{
				var st = new ExpressionStatement();
				st.Expression = ParseExpression(exs.Expression, semanticModel);
				return st;
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
			if(syntax.Variables.Count != 1)
			{
				throw new ParseException("変数の複数同時宣言は禁止です。");
			}

			var st = new VariableDeclarationStatement();

			var type = syntax.Type;
			var variable = syntax.Variables[0];

			if( variable.Initializer == null ||
				variable.Initializer.Value == null)
			{
				throw new ParseException("必ず変数は初期化する必要があります。");
			}

			var identifier = variable.Identifier;


			var argumentList = variable.ArgumentList;
			var initializer = variable.Initializer;

			st.Type = type;
			st.Name = identifier.ValueText;
			st.Value = ParseExpression(initializer.Value, semanticModel);

			return st;
		}

		public BlockStatement ParseBlockStatement(BlockSyntax syntax, SemanticModel semanticModel)
		{
			List<Statement> statements = new List<Statement>();

			foreach(var statement in syntax.Statements)
			{
				statements.Add(ParseStatement(statement, semanticModel));
			}

			var bs = new BlockStatement();
			bs.Statements = statements.ToArray();

			return bs;
		}
	}
}

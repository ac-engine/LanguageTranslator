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

			if (mae != null)
			{
				MemberAccessExpression exp = new MemberAccessExpression();

				TypeInfo? selfType = null;
				selfType = semanticModel.GetTypeInfo(mae);

				TypeInfo? parentType = null;
				if(mae.Expression != null) parentType = semanticModel.GetTypeInfo(mae.Expression);

				// 親の種類を探索
				EnumDef enumDefP = null;

				if (parentType.HasValue && parentType.Value.Type != null)
				{
					if(parentType.Value.Type.TypeKind == TypeKind.Enum)
					{
						var enumName = selfType.Value.Type.Name;
						var namespace_ = selfType.Value.Type.ContainingNamespace.ToString();
						enumDefP = definitions.Enums.Where(_ => _.Namespace == namespace_ && _.Name == enumName).FirstOrDefault();
					}
				}

				// 親から子を探索
	
				if(enumDefP != null)
				{
					var name = mae.Name.ToString();
					exp.EnumMember = enumDefP.Members.Where(_ => _.Name == name).FirstOrDefault();
				}
				else
				{
					if (selfType.HasValue && selfType.Value.Type != null)
					{
						if(selfType.Value.Type.TypeKind == TypeKind.Enum)
						{
							var enumName = selfType.Value.Type.Name;
							var namespace_ = selfType.Value.Type.ContainingNamespace.ToString();
							exp.Enum = definitions.Enums.Where(_ => _.Namespace == namespace_ && _.Name == enumName).FirstOrDefault();
						}
					}
				}

				if(mae.Expression != null)
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
			else if(ie != null)
			{
				return null;
			}

			return null;
		}
	}
}

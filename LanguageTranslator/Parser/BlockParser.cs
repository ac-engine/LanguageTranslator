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

			if (mae != null)
			{
				MemberAccessExpression ret = new MemberAccessExpression();

				TypeInfo? parentType = null;
				if (mae.Expression != null) parentType = semanticModel.GetTypeInfo(mae.Expression);

				if (parentType.HasValue)
				{
					if (parentType.Value.Type.TypeKind == TypeKind.Enum)
					{
						// Enum
						var name = parentType.Value.Type.Name;
						var namespace_ = parentType.Value.Type.ContainingNamespace.ToString();

						//var enumDef = definitions.GetEnum(name, namespace_);
					}
				}
				else
				{

				}

				return ret;
			}
			else if (le != null)
			{
				Console.WriteLine(le);
			}
			else
			{
				throw new Exception("対応していないExpressionSyntax");
			}

			return null;
		}
	}
}

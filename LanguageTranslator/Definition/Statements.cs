using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LanguageTranslator.Definition
{
	class BlockStatement : Statement
	{
		public Statement[] Statements;
	}

	class VariableDeclaration : Statement
	{
		/// <summary>
		/// 種類(仮)
		/// </summary>
		public TypeSyntax Type;

		/// <summary>
		/// 名称
		/// </summary>
		public string Name = string.Empty;

		/// <summary>
		/// 値
		/// </summary>
		public Expression Value = null;
	}


	class ForeachStatement : Statement
	{
		/// <summary>
		/// 種類(仮)
		/// </summary>
		public TypeSyntax Type;

		/// <summary>
		/// 名称
		/// </summary>
		public string Name = string.Empty;

		/// <summary>
		/// 値
		/// </summary>
		public Expression Value = null;

		/// <summary>
		/// 実行される内容
		/// </summary>
		public Statement Statement = null;
	}

	class ForStatement : Statement
	{
		/// <summary>
		/// 条件
		/// </summary>
		public Expression Condition = null;

		/// <summary>
		/// i++の部分
		/// </summary>
		/// <remarks>
		/// ,による接続不可
		/// </remarks>
		public Expression Incrementor = null;

		/// <summary>
		/// 実行される内容
		/// </summary>
		public Statement Statement = null;
	}

	class IfStatement : Statement
	{

		/// <summary>
		/// 条件
		/// </summary>
		public Expression Condition = null;

		/// <summary>
		/// 条件がtrueの時に実行される内容
		/// </summary>
		public Statement TrueStatement = null;

		/// <summary>
		/// 条件がfalseの時に実行される内容
		/// </summary>
		/// <remarks>
		/// else ifの場合はIfStatementが設定される。
		/// </remarks>
		public Statement FalseStatement = null;
	}

	class Statement
	{
	}
}

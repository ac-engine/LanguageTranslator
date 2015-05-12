using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Definition
{
	class BlockStatement : Statement
	{
		public Statement[] Statements;
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

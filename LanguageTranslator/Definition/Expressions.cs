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
	class MemberAccessExpression : Expression
	{
		//public EnumDef Enum = null;

		/// <summary>
		/// メンバーへのアクセスがenumのメンバーだった場合の値
		/// </summary>
		public EnumMemberDef EnumMember = null;

		public Expression Expression = null;
	}

	class CastExpression : Expression
	{
		/// <summary>
		/// 型
		/// </summary>
		public TypeSpecifier Type;
		public Expression Expression = null;
	}

	class LiteralExpression : Expression
	{
		public string Text;
	}

	class InvocationExpression : Expression
	{
		public Expression Method;
		public Expression[] Args;
	}

	class ObjectCreationExpression : Expression
	{
		/// <summary>
		/// 型
		/// </summary>
		public TypeSpecifier Type;

		public Expression[] Args;
	}

	/// <summary>
	/// Expressionかローカル変数に代入する。
	/// </summary>
	class AssignmentExpression : Expression
	{
		public Expression Target;
		public Expression Expression;
	}

	class ElementAccessExpression : Expression
	{
		public Expression Value;
		public Expression Arg;
	}

	class ThisExpression : Expression
	{

	}

	class IdentifierNameExpression : Expression
	{
		public string Name;
	}

	/// <summary>
	/// +等
	/// </summary>
	class BinaryExpression : Expression
	{
		public Expression Left;
		public Expression Right;
		public OperatorType Operator;

		public enum OperatorType
		{
			Add,
			Subtract,
		}
	}

	/// <summary>
	/// ++等
	/// </summary>
	class PrefixUnaryExpression : Expression
	{
		public Expression Expression;
		public OperatorType Type;

		public enum OperatorType
		{
			PlusPlus,
			MinusMinus,
		}
	}

	class Expression
	{

	}
}

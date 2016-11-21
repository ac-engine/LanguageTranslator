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
		public string Name = string.Empty;

		/// <summary>
		/// メンバーへのアクセスがクラスの保有する何かだった場合のclass
		/// </summary>
		public ClassDef Class = null;

		/// <summary>
		/// メンバーへのアクセスがStructの保有する何かだった場合のstruct
		/// </summary>
		public StructDef Struct = null;

		/// <summary>
		/// メンバーへのアクセスがenumのメンバーだった場合のenum
		/// </summary>
		public EnumDef Enum = null;

		/// <summary>
		/// メンバーへのアクセスがenumのメンバーだった場合のenumのメンバー
		/// </summary>
		/// <remarks>
		/// 必ずExpressionはnullになる。
		/// このフィールド
		/// </remarks>
		public EnumMemberDef EnumMember = null;

		/// <summary>
		/// メンバーへのアクセスがメソッドだった場合の値
		/// </summary>
		public MethodDef Method = null;

		/// <summary>
		/// メンバーへのアクセスがプロパティだった場合の値
		/// </summary>
		public PropertyDef Property = null;

		public bool IsProperty = false;

		/// <summary>
		/// ジェネリックだった場合
		/// </summary>
		public TypeSpecifier[] Types = new TypeSpecifier[0];

		public Expression Expression = null;
	}

	class GenericNameExpression : Expression
	{
		public string Name = string.Empty;

		public bool IsMethod = false;
		public bool IsProperty = false;

		public TypeSpecifier[] Types = new TypeSpecifier[0];
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
		public Expression[] Args = new Expression[0];
	}

	class ObjectCreationExpression : Expression
	{
		/// <summary>
		/// 型
		/// </summary>
		public TypeSpecifier Type;

		public Expression[] Args;
	}

	class ObjectArrayCreationExpression : Expression
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
		public object Temp;
		public OperatorType Type = OperatorType.None;

		public enum OperatorType
		{
			None,
			Simple,
			Add,
			Substract,
			Divide,
			Modulo,
		}
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
		public bool IsMethod = false;
		public bool IsProperty = false;
		public TypeSpecifier Type = null;
	}

	class TypeExpression : Expression
	{
	}

	/// <summary>
	/// +等
	/// </summary>
	class BinaryExpression : Expression
	{
		public Expression Left;
		public Expression Right;
		public OperatorType Operator = OperatorType.None;

		public enum OperatorType
		{
			None,

			Add,
			Subtract,
			As,
			Is,
			Equals,
			NotEquals,

			LogicalAnd,
			LogicalOr,

			GreaterThan,
			LessThan,

			GreaterThanOrEqual,
			LessThanOrEqual,

			Multiply,
			Divide,

			Modulo,
		}
	}

	/// <summary>
	/// ++等
	/// </summary>
	class PostfixUnaryExpression : Expression
	{
		public Expression Operand;
		public OperatorType Type = OperatorType.None;

		public enum OperatorType
		{
			None,
			PostIncrement,
			PostDecrement,
		}
	}

	/// <summary>
	/// !等
	/// </summary>
	class PrefixUnaryExpression : Expression
	{
		public Expression Expression;
		public OperatorType Type = OperatorType.None;

		public enum OperatorType
		{
			None,
			LogicalNot,
			UnaryPlus,
			UnaryMinus,
			PreIncrement,
		}
	}

	class AsExpression : Expression
	{

	}

	class BaseExpression : Expression
	{

	}

	class InitializerExpression : Expression
	{
		public Expression[] Expressions = new Expression[0];
	}

	class Expression
	{
		/// <summary>
		/// 自己の型
		/// </summary>
		public TypeSpecifier SelfType;
		internal ExpressionSyntax Internal = null;
	}
}

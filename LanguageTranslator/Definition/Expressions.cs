﻿using System;
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
		public EnumDef Enum = null;
		public EnumMemberDef EnumMember = null;

		public Expression Expression = null;
	}

	class CastExpression : Expression
	{
		/// <summary>
		/// 種類(仮)
		/// </summary>
		public TypeSyntax Type;

		public Expression Expression = null;
	}

	class LiteralExpression : Expression
	{
		public string Text;
	}

	class InvocationExpression : Expression
	{

	}

	class ObjectCreation : Expression
	{

	}

	class Expression
	{

	}
}

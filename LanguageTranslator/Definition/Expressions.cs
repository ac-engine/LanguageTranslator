using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Definition
{
	class MemberAccessExpression : Expression
	{
		public EnumDef Enum = null;
		public EnumMemberDef EnumMember = null;

		public Expression Expression = null;
	}

	class LiteralExpression : Expression
	{
		public string Text;
	}

	class InvocationExpression : Expression
	{

	}

	class Expression
	{

	}
}

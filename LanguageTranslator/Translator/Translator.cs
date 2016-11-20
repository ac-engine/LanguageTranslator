using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Translator
{
	abstract class Translator
	{
		public abstract void Translate(string targetDir, Definition.Definitions definisions);

		public virtual string GetLiteral(string text) { return text; }

		public virtual string GetStringLiteral(string text)
		{
			return "\"" + text + "\"";
		}

		public virtual string GetRawLiteral(string text)
		{
			return "@\"" + text + "\"";
		}

		protected string ParseLiteralExpression(Definition.LiteralExpression e)
		{
			if (e.Text.StartsWith("@"))
			{
				var last = e.Text.LastIndexOf('\"');

				return GetRawLiteral(e.Text.Substring(2, last + 1 - 3));
			}
			else if (e.Text.StartsWith("\""))
			{
				var last = e.Text.LastIndexOf('\"');

				return GetStringLiteral(e.Text.Substring(1, last + 1 - 2));
			}
			else
			{
				return GetLiteral(e.Text);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Translator
{
	interface ITranslator
	{
		void Translate(string targetDir, Definition.Definitions definisions);
	}
}

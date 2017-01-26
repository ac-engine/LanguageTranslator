using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Translator.Cpp
{
	class Translator : LanguageTranslator.Translator.Translator
	{
		const string PackageName = "asd";

		void Summary(Definition.SummaryComment summary)
		{
			if (string.IsNullOrEmpty(summary.Summary)) return;

			WriteLine("/**");
			WriteLine("@brief {0}", summary.Summary);

			foreach (var p in summary.ParamComments)
			{
				WriteLine("@param {0} {1}", p.Name, p.Comment);
			}

			WriteLine("*/");
		}


		public override void Translate(string targetDir, Definition.Definitions definisions)
		{
		}
	}
}

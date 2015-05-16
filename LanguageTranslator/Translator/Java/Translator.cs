using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Translator.Java
{
	class Translator : ITranslator
	{
		private StringBuilder Res = new StringBuilder();
		private int IndentDepth = 0;
		private void MakeIndent()
		{
			Res.Append('\t', IndentDepth);
		}

		private void MakeBrief(string brief)
		{
			if (String.IsNullOrEmpty(brief))
			{
				return;
			}
			MakeIndent();
			Res.AppendFormat("/* {0} */\n", brief);
		}

		private void OutputEnum(Definition.EnumDef es)
		{
			MakeBrief(es.Brief);
			MakeIndent();
			Res.AppendFormat("enum {0} {{\n", es.Name);
			IndentDepth++;
			foreach (var e in es.Members) 
			{
				MakeBrief(e.Brief);
				MakeIndent();
				Res.AppendFormat("{0} = {1},\n", e.Name, e.Value.ToString());
			}
			IndentDepth--;
			MakeIndent();
			Res.AppendFormat("}}");
		}


		private void OutputClass(Definition.ClassDef cs)
		{
			MakeBrief(cs.Brief);
			MakeIndent();
			Res.AppendFormat("class {0} {{\n", cs.Name);
			IndentDepth++;
			foreach (var p in cs.Properties)
			{
				MakeBrief(p.Brief);
				MakeIndent();
				Res.AppendFormat("private {0} {1};", p.);
			}
			IndentDepth--;
			MakeIndent();
			Res.AppendFormat("}}");
		}
		public void Translate(string targetDir, Definition.Definitions definisions)
		{
			foreach (Definition.EnumDef e in definisions.Enums)
			{
				OutputEnum(e);
			}

			Console.Write(Res.ToString());
			Console.ReadLine();
		}
	}
}

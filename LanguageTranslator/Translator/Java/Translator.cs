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

		private string GetExpression(Definition.Expression e)
		{
			// not implemented
			return "";
		}

		private void OutputStatement(Definition.Statement s)
		{
			// not implemented
		}


		public string GetParamStr(List<Definition.ParameterDef> ps)
		{
			var isFirst = true;
			var res = new StringBuilder();
			foreach (var p in ps)
			{
				if (isFirst)
				{
					isFirst = false;
				}
				else
				{
					res.Append(", ");
				}
				res.Append(p.Type);
				res.Append(" ");
				res.Append(p.Name);
			}
			return res.ToString();
		}

		private void OutputEnum(Definition.EnumDef es)
		{
			MakeBrief(es.Brief);
			MakeIndent();
			Res.AppendFormat("public enum {0} {{\n", es.Name);
			IndentDepth++;
			foreach (var e in es.Members)
			{
				MakeBrief(e.Brief);
				MakeIndent();
				Res.AppendFormat("{0} = {1},\n", e.Name, GetExpression(e.Value));
			}
			IndentDepth--;
			MakeIndent();
			Res.AppendFormat("}}\n");
		}


		private void OutputClass(Definition.ClassDef cs)
		{
			MakeBrief(cs.Brief);
			MakeIndent();
			Res.AppendFormat("public class {0} {{\n", cs.Name);
			IndentDepth++;


			foreach (var f in cs.Fields)
			{
				MakeBrief(f.Brief);
				MakeIndent();
				Res.AppendFormat("public {0} {1} = {2};\n", f.Type, f.Name, GetExpression(f.Initializer));
			}

			foreach (var p in cs.Properties)
			{
				MakeBrief(p.Brief);
				MakeIndent();
				Res.AppendFormat("private {0} {1};\n", p.Type, p.Name);
				MakeIndent();

				Res.AppendFormat("public void set{0} {{\n", p.Name);
				IndentDepth++;
				if (p.Setter.Body != null)
				{
					OutputStatement(p.Setter.Body);
				}
				IndentDepth--;
				MakeIndent();
				Res.AppendLine("}");

				Res.AppendFormat("public void get{0} {{\n", p.Name);
				IndentDepth++;
				if (p.Getter.Body != null)
				{
					OutputStatement(p.Getter.Body);
				}
				IndentDepth--;
				Res.AppendLine("}");
			}

			foreach (var m in cs.Methods)
			{
				MakeBrief(m.Brief);
				MakeIndent();

				Res.AppendFormat("public {0} {1}({2}) {{\n", m.ReturnType, m.Name, GetParamStr(m.Parameters));
				IndentDepth++;
				foreach (var s in m.Body)
				{
					OutputStatement(s);
				}
				IndentDepth--;
				Res.AppendLine("}");
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

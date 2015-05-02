using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LanguageTranslator.CodeCommentParser
{
	class Parser
	{
		public void Parse(string xmlPath, Definition.Definitions definisions)
		{
			var document = XDocument.Load(xmlPath);
			
			var summaries = document.Descendants("member")
				.Select(m => new { Address = m.Attribute("name").Value.ToString(), Summary = m.Elements("summary") })
				.Where(fs => fs.Summary.Any())
				.ToDictionary(fs => fs.Address, fs => fs.Summary.Single().Value.ToString());

			definisions.Classes.ForEach(c => { if (summaries.ContainsKey(c.Name)) c.Brief = summaries[c.Name]; });
			definisions.Enums.ForEach(e => { if (summaries.ContainsKey(e.Name)) e.Brief = summaries[e.Name]; });
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator
{
	[DataContract]
	class Settings
	{
		[DataMember]
		public string ExportFilePath { get; set; }
	}
}

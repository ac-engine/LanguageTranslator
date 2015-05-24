using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace LanguageTranslator
{
	class Program
	{
		static void Main(string[] args)
		{
			/*
			if(args.Length < 1)
			{
				Console.WriteLine("第１引数に設定ファイルを指定してください");
				return;
			}

			Settings settings;
			var settingFilePath = args[0];
			var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Settings));
			using (var file = File.Open(settingFilePath, FileMode.Open))
			{
				settings = serializer.ReadObject(file) as Settings;
			}

			var settingsDirectory = Path.GetDirectoryName(args[0]);
			*/

			var csharpDir = "ace_cs/";
			var dstDir = "ace_java/";

			var parser = new Parser.Parser();
			var cs = Directory.EnumerateFiles(csharpDir, "*.cs", SearchOption.AllDirectories).ToArray();

			parser.TypesWhosePrivateNotParsed.Add("ace.Particular.GC");
			parser.TypesWhosePrivateNotParsed.Add("ace.Particular.Helper");

			Definition.Definitions definitions = null;
			
			try
			{
				definitions = parser.Parse(cs);
			}
			catch(Parser.ParseException e)
			{
				Console.WriteLine(e.Message);
				return;
			}
			
			// コードコメントxmlの解析
			var xmlPath = string.Empty;

			if(System.IO.File.Exists(xmlPath))
			{
				var codeCommentParser = new CodeCommentParser.Parser();
				codeCommentParser.Parse(xmlPath, definitions);
			}
			
			// 変換後コードの出力
			var translator = new Translator.Java.Translator();

			System.IO.Directory.CreateDirectory(dstDir);
			translator.Translate(dstDir, definitions);
		}
	}



}

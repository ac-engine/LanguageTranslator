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

			Editor editor = new Editor(definitions);

			editor.AddTypeConverter("System", "Void", "", "void");
			editor.AddTypeConverter("System", "Boolean", "", "bool");
			editor.AddTypeConverter("System", "Int32", "", "int");
			editor.AddTypeConverter("System", "Single", "", "float");
			editor.AddTypeConverter("System", "Byte", "", "byte");
			editor.AddTypeConverter("System.Collections.Generic", "List", "java.util", "ArrayList");

			editor.Convert();
			
			// 変換後コードの出力
			var translator = new Translator.Java.Translator();

			System.IO.Directory.CreateDirectory(dstDir);
			translator.Translate(dstDir, definitions);
		}
	}

	class Editor
	{
		Definition.Definitions definitions;

		Dictionary<string, Tuple<string, string>> typeConverter = new Dictionary<string, Tuple<string, string>>();

		public Editor(Definition.Definitions definitions)
		{
			this.definitions = definitions;
		}

		public void AddTypeConverter(string fromNamespace, string fromType, string toNamespace, string toType)
		{
			string from = GetTypeString(fromNamespace, fromType);
			typeConverter.Add(from, Tuple.Create(toNamespace, toType));
		}

		public void Convert()
		{
			foreach(var structDef in definitions.Structs)
			{
				foreach(var field in structDef.Fields)
				{
					field.Type = ConvertType(field.Type);
				}

				foreach(var prop in structDef.Properties)
				{
					prop.Type = ConvertType(prop.Type);
				}

				foreach(var method in structDef.Methods)
				{
					foreach(var paramDef in method.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					method.ReturnType = ConvertType(method.ReturnType);
				}
			}
		}

		string GetTypeString(string namespace_, string type_)
		{
			string typeString = string.Empty;
			if (string.IsNullOrEmpty(namespace_))
			{
				typeString = type_;
			}
			else
			{
				typeString = namespace_ + "." + type_;
			}

			return typeString;
		}

		Definition.TypeSpecifier ConvertType(Definition.TypeSpecifier typeSpecifier)
		{
			if(typeSpecifier is Definition.SimpleType)
			{
				var src = typeSpecifier as Definition.SimpleType;
				var dst = new Definition.SimpleType();

				string srcType = GetTypeString(src.Namespace, src.TypeName);

				if(typeConverter.ContainsKey(srcType))
				{
					var dstType = typeConverter[srcType];
					dst.Namespace = dstType.Item1;
					dst.TypeName = dstType.Item2;
				}
				else
				{
					dst.TypeName = src.TypeName;
					dst.Namespace = src.Namespace;
				}
				
				return dst;
			}
			else if(typeSpecifier is Definition.ArrayType)
			{
				var src = typeSpecifier as Definition.ArrayType;
				var dst = new Definition.ArrayType();

				dst.BaseType = ConvertType(src.BaseType) as Definition.SimpleType;

				return dst;
			}
			else if(typeSpecifier is Definition.GenericType)
			{
				var src = typeSpecifier as Definition.GenericType;
				var dst = new Definition.GenericType();

				dst.OuterType = ConvertType(src.OuterType) as Definition.SimpleType;
				dst.InnerType = src.InnerType.Select(_ => ConvertType(_) as Definition.SimpleType).ToList();

				return dst;
			}

			return typeSpecifier;
		}

		
	}

}

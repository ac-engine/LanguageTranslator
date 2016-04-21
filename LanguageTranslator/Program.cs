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
			var csharpDir = "asd_cs/";
			var dstDir = "asd_java/";

			if(args.Length >= 2)
			{
				csharpDir = args[0];
				dstDir = args[1];
			}

			var parser = new Parser.Parser();
			var cs = Directory.EnumerateFiles(csharpDir, "*.cs", SearchOption.AllDirectories).ToArray();

			parser.TypesWhosePrivateNotParsed.Add("asd.Particular.GC");
			parser.TypesWhosePrivateNotParsed.Add("asd.Particular.Helper");
			parser.TypesWhosePrivateNotParsed.Add("asd.Particular.Dictionary");
			parser.TypesWhoseMemberNotParsed.Add("asd.Particular.Lambda");
			parser.TypesWhoseMemberNotParsed.Add("asd.Particular.Define");

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

	

			editor.AddMethodConverter("System.Collections.Generic", "List", "Add", "add");
			editor.AddMethodConverter("System.Collections.Generic", "List", "Clear", "clear");

			editor.AddMethodConverter("System.Collections.Generic", "LinkedList", "AddLast", "add");
			editor.AddMethodConverter("System.Collections.Generic", "LinkedList", "Contains", "contains");
			editor.AddMethodConverter("System.Collections.Generic", "LinkedList", "Clear", "clear");

			editor.AddMethodConverter("System.Collections.Generic", "Dictionary", "Add", "put");
			editor.AddMethodConverter("System.Collections.Generic", "Dictionary", "ContainsKey", "containsKey");
			editor.AddMethodConverter("System.Collections.Generic", "Dictionary", "Remove", "remove");
			editor.AddMethodConverter("System.Collections.Generic", "Dictionary", "Clear", "clear");


			editor.AddMethodConverter("System", "Math", "Sqrt", "sqrt");
			
			editor.AddTypeConverter("System", "Void", "", "void");
			editor.AddTypeConverter("System", "Boolean", "", "boolean");
			editor.AddTypeConverter("System", "Int32", "", "int");
			editor.AddTypeConverter("System", "Single", "", "float");
			editor.AddTypeConverter("System", "Double", "", "double");
			editor.AddTypeConverter("System", "Byte", "", "byte");

			editor.AddTypeConverter("System", "Object", "java.lang", "Object");

			editor.AddTypeConverter("System", "IntPtr", "", "long");

			editor.AddTypeConverter("System", "String", "java.lang", "String");

			editor.AddTypeConverter("System.Collections.Generic", "List", "java.util", "ArrayList");
			editor.AddTypeConverter("System.Collections.Generic", "LinkedList", "java.util", "LinkedList");
			editor.AddTypeConverter("System.Collections.Generic", "Dictionary", "java.util", "Map");
			editor.AddTypeConverter("System.Collections.Generic", "SortedList", "java.util", "SortedMap");
			editor.AddTypeConverter("System.Collections.Generic", "KeyValuePair", "java.util", "Map.Entry");
			editor.AddTypeConverter("System.Collections.Generic", "IEnumerable", "java.lang", "Iterable");

			editor.AddTypeConverter("System", "Math", "java.lang", "Math");

			editor.AddTypeConverter("System", "WeakReference", "java.lang.ref", "WeakReference");

			editor.AddIgnoredType("asd.Particular", "Dictionary");
			editor.AddIgnoredType("asd.Particular", "GC");
			editor.AddIgnoredType("asd.Particular", "Helper");
			editor.AddIgnoredType("asd.Particular", "Lambda");
			editor.AddIgnoredType("asd.Particular", "Define");

			{
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var ae = o as Definition.AssignmentExpression;

					if(ae != null)
					{
						var target = ae.Target as Definition.MemberAccessExpression;
						if(target != null && target.Property != null)
						{
							// setter差し替え
							var invocation = new Definition.InvocationExpression();

							// 関数設定
							var memf = new Definition.MemberAccessExpression();
							memf.Method = new Definition.MethodDef();
							memf.Method.Name = "set" + target.Property.Name;
							memf.Expression = target.Expression;

							invocation.Method = memf;

							// 引数設定
							invocation.Args = new[] { ae.Expression };
							
							return Tuple.Create<bool, object>(false, invocation);
						}
					}

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}

			{
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var mae = o as Definition.MemberAccessExpression;

					if (mae != null && mae.Property != null)
					{
						// getter差し替え
						var invocation = new Definition.InvocationExpression();

						// 関数設定
						var memf = new Definition.MemberAccessExpression();
						memf.Method = new Definition.MethodDef();
						memf.Method.Name = "get" + mae.Property.Name;
						memf.Expression = mae.Expression;
						invocation.Method = memf;

						// 引数設定
						invocation.Args = new Definition.Expression[0];

						return Tuple.Create<bool, object>(true, invocation);
						
					}

					var ime = o as Definition.IdentifierNameExpression;
					if (ime != null && ime.IsProperty)
					{
						// getter差し替え
						var invocation = new Definition.InvocationExpression();

						// 関数設定
						var memf = new Definition.MemberAccessExpression();
						memf.Method = new Definition.MethodDef();
						memf.Method.Name = "get" + ime.Name;
						invocation.Method = memf;

						// 引数設定
						invocation.Args = new Definition.Expression[0];

						return Tuple.Create<bool, object>(true, invocation);
					}

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}

			// 絶対ネームスペースに変換
			{
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var id = o as Definition.IdentifierNameExpression;
					if(id != null && id.Name == "Particular")
					{
						var mae = new Definition.MemberAccessExpression();
						mae.Name = "Particular";
						mae.Expression = id;
						id.Name = "asd";
						return Tuple.Create<bool, object>(false, mae);
					}

					if (id != null && id.Name == "swig")
					{
						var mae = new Definition.MemberAccessExpression();
						mae.Name = "swig";
						mae.Expression = id;
						id.Name = "asd";
						return Tuple.Create<bool, object>(false, mae);
					}

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}

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

		HashSet<Func<object, Tuple<bool, object>>> editFunc = new HashSet<Func<object, Tuple<bool, object>>>();

		Dictionary<string, Tuple<string, string>> typeConverter = new Dictionary<string, Tuple<string, string>>();

		Dictionary<string, string> methodConverter = new Dictionary<string, string>();

		HashSet<Tuple<string, string>> ignoreTypes = new HashSet<Tuple<string, string>>();

		public Editor(Definition.Definitions definitions)
		{
			this.definitions = definitions;
		}

		public void AddIgnoredType(string ns, string type)
		{
			ignoreTypes.Add(Tuple.Create(ns, type));
		}

		public void AddTypeConverter(string fromNamespace, string fromType, string toNamespace, string toType)
		{
			string from = GetTypeString(fromNamespace, fromType);
			typeConverter.Add(from, Tuple.Create(toNamespace, toType));
		}

		public void AddMethodConverter(string namespace_, string type_, string fromMethod, string toMethod)
		{
			var methodString = GetTypeString(namespace_, type_) + "." + fromMethod;
			methodConverter.Add(methodString, toMethod);
		}

		public void AddEditFunc(Func<object, Tuple<bool, object>> func)
		{
			editFunc.Add(func);
		}

		public void Convert()
		{
			Edit();
			ConvertMethod();
			ConvertTypeName();
			RemoveType();
		}

		void RemoveType()
		{
			foreach (var it in ignoreTypes)
			{
				definitions.Classes.RemoveAll(_ => _.Namespace == it.Item1 && _.Name == it.Item2);
				definitions.Structs.RemoveAll(_ => _.Namespace == it.Item1 && _.Name == it.Item2);
				definitions.Enums.RemoveAll(_ => _.Namespace == it.Item1 && _.Name == it.Item2);
				definitions.Interfaces.RemoveAll(_ => _.Namespace == it.Item1 && _.Name == it.Item2);
			}
		}

		void Edit()
		{
			foreach(var func in editFunc)
			{
				Edit(func, definitions.Classes);
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, List<Definition.ClassDef> arr)
		{
			for (int i = 0; i < arr.Count; i++)
			{
				var r = func(arr[i]);
				if (r.Item2 != null)
				{
					arr[i] = (Definition.ClassDef)r.Item2;
					if (!r.Item1) continue;
				}

				Edit(func, arr[i].Methods);
				Edit(func, arr[i].Fields);
				Edit(func, arr[i].Properties);
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, List<Definition.MethodDef> arr)
		{
			for (int i = 0; i < arr.Count; i++)
			{
				var r = func(arr[i]);
				if (r.Item2 != null)
				{
					arr[i] = (Definition.MethodDef)r.Item2;
					if (!r.Item1) continue;
				}

				Edit(func, arr[i].Body);
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, List<Definition.FieldDef> arr)
		{
			for (int i = 0; i < arr.Count; i++)
			{
				var r = func(arr[i]);
				if (r.Item2 != null)
				{
					arr[i] = (Definition.FieldDef)r.Item2;
					if (!r.Item1) continue;
				}
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, List<Definition.PropertyDef> arr)
		{
			for (int i = 0; i < arr.Count; i++)
			{
				var r = func(arr[i]);
				if (r.Item2 != null)
				{
					arr[i] = (Definition.PropertyDef)r.Item2;
					if (!r.Item1) continue;
				}

				if (arr[i].Getter != null)
				{
					Edit(func, ref arr[i].Getter.Body);
				}

				if (arr[i].Setter != null)
				{
					Edit(func, ref arr[i].Setter.Body);
				}
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, ref Definition.Statement arr)
		{
			var r = func(arr);
			if (r.Item2 != null)
			{
				arr = (Definition.Statement)r.Item2;

				if (!r.Item1) return;
			}

			var s = arr;

			if (s == null)
			{
			}
			else if (s is Definition.BlockStatement)
			{
				var s_ = s as Definition.BlockStatement;
				Edit(func, s_.Statements);
			}
			else if (s is Definition.VariableDeclarationStatement)
			{
				var s_ = s as Definition.VariableDeclarationStatement;
				Edit(func, ref s_.Type);
				Edit(func, ref s_.Value);
			}
			else if (s is Definition.ForeachStatement)
			{
				var s_ = s as Definition.ForeachStatement;
				Edit(func, ref s_.Type);
				Edit(func, ref s_.Value);
				Edit(func, ref s_.Statement);
			}
			else if (s is Definition.ForStatement)
			{
				var s_ = s as Definition.ForStatement;

				{
					Definition.Statement st = s_.Declaration;
					Edit(func, ref st);
					s_.Declaration = st as Definition.VariableDeclarationStatement;
				}

				Edit(func, ref s_.Condition);
				Edit(func, ref s_.Incrementor);
				Edit(func, ref s_.Statement);
			}
			else if (s is Definition.IfStatement)
			{
				var s_ = s as Definition.IfStatement;
				Edit(func, ref s_.Condition);
				Edit(func, ref s_.TrueStatement);
				Edit(func, ref s_.FalseStatement);
			}
			else if (s is Definition.ReturnStatement)
			{
				var s_ = s as Definition.ReturnStatement;
				Edit(func, ref s_.Return);
			}
			else if (s is Definition.ContinueStatement)
			{
			}
			else if (s is Definition.ExpressionStatement)
			{
				var s_ = s as Definition.ExpressionStatement;
				Edit(func, ref s_.Expression);
			}
			else if (s is Definition.LockStatement)
			{
				var s_ = s as Definition.LockStatement;
				Edit(func, ref s_.Expression);
				Edit(func, ref s_.Statement);
			}
			else
			{
				throw new Exception();
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, ICollection<Definition.Statement> arr)
		{

			for (int i = 0; i < arr.Count; i++)
			{
				var r = func(arr.ElementAt(i));
				if (r.Item2 != null)
				{
					if (arr is Definition.Statement[])
					{
						var arrr = ((Definition.Statement[])arr)[i];
						((Definition.Statement[])arr)[i] = (Definition.Statement)r.Item2;
					}

					if (arr is List<Definition.Statement>)
					{
						((List<Definition.Statement>)arr)[i] = (Definition.Statement)r.Item2;
					}

					if (!r.Item1) continue;
				}

				var s = arr.ElementAt(i);

				if(s == null)
				{

				}
				else if (s is Definition.BlockStatement)
				{
					var s_ = s as Definition.BlockStatement;
					Edit(func, s_.Statements);
				}
				else if (s is Definition.VariableDeclarationStatement)
				{
					var s_ = s as Definition.VariableDeclarationStatement;
					Edit(func, ref s_.Type);
					Edit(func, ref s_.Value);
				}
				else if (s is Definition.ForeachStatement)
				{
					var s_ = s as Definition.ForeachStatement;
					Edit(func, ref s_.Type);
					Edit(func, ref s_.Value);
					Edit(func, ref s_.Statement);
				}
				else if (s is Definition.ForStatement)
				{
					var s_ = s as Definition.ForStatement;

					{
						Definition.Statement st = s_.Declaration;
						Edit(func, ref st);
						s_.Declaration = st as Definition.VariableDeclarationStatement;
					}
					
					Edit(func, ref s_.Condition);
					Edit(func, ref s_.Incrementor);
					Edit(func, ref s_.Statement);
				}
				else if (s is Definition.IfStatement)
				{
					var s_ = s as Definition.IfStatement;
					Edit(func, ref s_.Condition);
					Edit(func, ref s_.TrueStatement);
					Edit(func, ref s_.FalseStatement);
				}
				else if (s is Definition.ReturnStatement)
				{
					var s_ = s as Definition.ReturnStatement;
					Edit(func, ref s_.Return);
				}
				else if (s is Definition.ContinueStatement)
				{
				}
				else if (s is Definition.ExpressionStatement)
				{
					var s_ = s as Definition.ExpressionStatement;
					Edit(func, ref s_.Expression);
				}
				else if (s is Definition.LockStatement)
				{
					var s_ = s as Definition.LockStatement;
					Edit(func, ref s_.Expression);
					Edit(func, ref s_.Statement);
				}
				else
				{
					throw new Exception();
				}
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, ref Definition.Expression arr)
		{
			var r = func(arr);

			if (r.Item2 != null)
			{
				arr = (Definition.Expression)r.Item2;

				if (!r.Item1) return;
			}

			var e = arr;

			if (e == null)
			{
				return;
			}
			else if (e is Definition.MemberAccessExpression)
			{
				var e_ = e as Definition.MemberAccessExpression;
				Edit(func, ref e_.Expression);
			}
			else if (e is Definition.GenericMemberAccessExpression)
			{
				var e_ = e as Definition.GenericMemberAccessExpression;
				for (int i = 0; i < e_.Types.Length; i++)
				{
					Edit(func, ref e_.Types[i]);
				}
			}
			else if (e is Definition.CastExpression)
			{
				var e_ = e as Definition.CastExpression;
				e_.Type = ConvertType(e_.Type);
				Edit(func, ref e_.Expression);
			}
			else if (e is Definition.LiteralExpression)
			{
			}
			else if (e is Definition.InvocationExpression)
			{
				var e_ = e as Definition.InvocationExpression;
				Edit(func, ref e_.Method);
				for (int i = 0; i < e_.Args.Length; i++ )
				{
					Edit(func, ref e_.Args[i]);
				}
			}
			else if (e is Definition.ObjectCreationExpression)
			{
				var e_ = e as Definition.ObjectCreationExpression;
				e_.Type = ConvertType(e_.Type);
				for (int i = 0; i < e_.Args.Length; i++)
				{
					Edit(func, ref e_.Args[i]);
				}
			}
			else if (e is Definition.AssignmentExpression)
			{
				var e_ = e as Definition.AssignmentExpression;
				Edit(func, ref e_.Target);
				Edit(func, ref e_.Expression);
			}
			else if (e is Definition.ElementAccessExpression)
			{
				var e_ = e as Definition.ElementAccessExpression;
				Edit(func, ref e_.Value);
				Edit(func, ref e_.Arg);
			}
			else if (e is Definition.ThisExpression)
			{
			}
			else if (e is Definition.IdentifierNameExpression)
			{
				var e_ = e as Definition.IdentifierNameExpression;
			}
			else if (e is Definition.BinaryExpression)
			{
				var e_ = e as Definition.BinaryExpression;
				Edit(func, ref e_.Left);
				Edit(func, ref e_.Right);
			}
			else if (e is Definition.PrefixUnaryExpression)
			{
				var e_ = e as Definition.PrefixUnaryExpression;
				Edit(func, ref e_.Expression);
			}
			else if (e is Definition.PostfixUnaryExpression)
			{
				var e_ = e as Definition.PostfixUnaryExpression;
				Edit(func, ref e_.Operand);
			}
			else if (e is Definition.BaseExpression)
			{
			}
			else if (e is Definition.ObjectArrayCreationExpression)
			{
				var e_ = e as Definition.ObjectArrayCreationExpression;
				Edit(func, ref e_.Type);
				for (int i = 0; i < e_.Args.Length; i++)
				{
					Edit(func, ref e_.Args[i]);
				}
			}
			else if (e is Definition.TypeExpression)
			{
				var e_ = e as Definition.TypeExpression;
				Edit(func, ref e_.Type);
			}
			else
			{
				throw new Exception();
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, ref Definition.TypeSpecifier arr)
		{
			var r = func(arr);

			if (r.Item2 != null)
			{
				arr = (Definition.TypeSpecifier)r.Item2;

				if (!r.Item1) return;
			}

			var typeSpecifier = arr;

			if(typeSpecifier is Definition.SimpleType)
			{
				return;
			}
			else if(typeSpecifier is Definition.ArrayType)
			{
				var src = typeSpecifier as Definition.ArrayType;
				Definition.TypeSpecifier t = src.BaseType;
				Edit(func, ref t);
				src.BaseType = (Definition.SimpleType)t;
			}
			else if(typeSpecifier is Definition.GenericType)
			{
				var src = typeSpecifier as Definition.GenericType;

				{
					Definition.TypeSpecifier t = src.OuterType;
					Edit(func, ref t);
					src.OuterType = (Definition.SimpleType)t;
				}

				for (int i = 0; i < src.InnerType.Count(); i++)
				{
					Definition.TypeSpecifier t = src.InnerType[i];
					Edit(func, ref t);
					src.InnerType[i] = t;
				}
			}
			else if (typeSpecifier is Definition.GenericTypenameType)
			{
			}
			else
			{
				throw new Exception();
			}
		}

		void ConvertMethod()
		{
			foreach (var def in definitions.Structs)
			{
				foreach (var m in def.Constructors)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Destructors)
				{
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Methods)
				{
					var methodString = GetTypeString(def.Namespace, def.Name) + "." + m.Name;
					if(methodConverter.ContainsKey(methodString))
					{
						m.Name = methodConverter[methodString];
					}
				}
			}

			foreach (var def in definitions.Classes)
			{
				foreach (var m in def.Constructors)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Destructors)
				{
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Methods)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.ReturnType = ConvertType(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}
			}

			foreach (var def in definitions.Interfaces)
			{
				foreach (var m in def.Methods)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.ReturnType = ConvertType(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}
			}
		}

		void ConvertTypeName()
		{
			foreach(var def in definitions.Structs)
			{
				{
					var typeString = GetTypeString(def.Namespace, def.Name);
					if(typeConverter.ContainsKey(typeString))
					{
						def.Namespace = typeConverter[typeString].Item1;
						def.Name = typeConverter[typeString].Item2;
					}
				}

				foreach (var field in def.Fields)
				{
					field.Type = ConvertType(field.Type);
					field.Initializer = ConvertExpression(field.Initializer);
				}

				foreach (var prop in def.Properties)
				{
					prop.Type = ConvertType(prop.Type);
					if (prop.Getter != null)
					{
						prop.Getter.Body = ConvertStatement(prop.Getter.Body);
					}

					if (prop.Setter != null)
					{
						prop.Setter.Body = ConvertStatement(prop.Setter.Body);
					}
				}

				foreach (var m in def.Operators)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.ReturnType = ConvertType(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Constructors)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Destructors)
				{
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Methods)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.ReturnType = ConvertType(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}
			}

			foreach (var def in definitions.Classes)
			{
				{
					var typeString = GetTypeString(def.Namespace, def.Name);
					if (typeConverter.ContainsKey(typeString))
					{
						def.Namespace = typeConverter[typeString].Item1;
						def.Name = typeConverter[typeString].Item2;
					}
				}

				foreach (var field in def.Fields)
				{
					field.Type = ConvertType(field.Type);
					field.Initializer = ConvertExpression(field.Initializer);
				}

				foreach (var prop in def.Properties)
				{
					prop.Type = ConvertType(prop.Type);
					if (prop.Getter != null)
					{
						prop.Getter.Body = ConvertStatement(prop.Getter.Body);
					}

					if (prop.Setter != null)
					{
						prop.Setter.Body = ConvertStatement(prop.Setter.Body);
					}
				}

				foreach (var m in def.Operators)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.ReturnType = ConvertType(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Constructors)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Destructors)
				{
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}

				foreach (var m in def.Methods)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.ReturnType = ConvertType(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}
			}

			foreach (var def in definitions.Interfaces)
			{
				{
					var typeString = GetTypeString(def.Namespace, def.Name);
					if (typeConverter.ContainsKey(typeString))
					{
						def.Namespace = typeConverter[typeString].Item1;
						def.Name = typeConverter[typeString].Item2;
					}
				}

				foreach (var prop in def.Properties)
				{
					prop.Type = ConvertType(prop.Type);
					if (prop.Getter != null)
					{
						prop.Getter.Body = ConvertStatement(prop.Getter.Body);
					}

					if (prop.Setter != null)
					{
						prop.Setter.Body = ConvertStatement(prop.Setter.Body);
					}
				}

				foreach (var m in def.Methods)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertType(paramDef.Type);
					}

					m.ReturnType = ConvertType(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertStatement(_)).ToList();
				}
			}

			foreach(var def in definitions.Enums)
			{
				{
					var typeString = GetTypeString(def.Namespace, def.Name);
					if (typeConverter.ContainsKey(typeString))
					{
						def.Namespace = typeConverter[typeString].Item1;
						def.Name = typeConverter[typeString].Item2;
					}
				}

				foreach(var m in def.Members)
				{
					m.Value = ConvertExpression(m.Value);
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

		Definition.Statement ConvertStatement(Definition.Statement s)
		{
			if (s == null) return s;

			if (s is Definition.BlockStatement)
			{
				var s_ = s as Definition.BlockStatement;
				s_.Statements = s_.Statements.Select(_ => ConvertStatement(_)).ToArray();
				return s_;
			}
			else if (s is Definition.VariableDeclarationStatement)
			{
				var s_ = s as Definition.VariableDeclarationStatement;
				s_.Type = ConvertType(s_.Type);
				s_.Value = ConvertExpression(s_.Value);
				return s_;
			}
			else if (s is Definition.ForeachStatement)
			{
				var s_ = s as Definition.ForeachStatement;
				s_.Type = ConvertType(s_.Type);
				s_.Value = ConvertExpression(s_.Value);
				s_.Statement = ConvertStatement(s_.Statement);
				return s_;
			}
			else if (s is Definition.ForStatement)
			{
				var s_ = s as Definition.ForStatement;
				s_.Declaration = ConvertStatement(s_.Declaration) as Definition.VariableDeclarationStatement;
				s_.Condition = ConvertExpression(s_.Condition);
				s_.Incrementor = ConvertExpression(s_.Incrementor);
				s_.Statement = ConvertStatement(s_.Statement);
				return s_;
			}
			else if (s is Definition.IfStatement)
			{
				var s_ = s as Definition.IfStatement;
				s_.Condition = ConvertExpression(s_.Condition);
				s_.TrueStatement = ConvertStatement(s_.TrueStatement);
				s_.FalseStatement = ConvertStatement(s_.FalseStatement);
				return s_;
			}
			else if (s is Definition.ReturnStatement)
			{
				var s_ = s as Definition.ReturnStatement;
				s_.Return = ConvertExpression(s_.Return);
				return s_;
			}
			else if (s is Definition.ContinueStatement)
			{
				return s;
			}
			else if (s is Definition.ExpressionStatement)
			{
				var s_ = s as Definition.ExpressionStatement;
				s_.Expression = ConvertExpression(s_.Expression);
				return s_;
			}
			else if (s is Definition.LockStatement)
			{
				var s_ = s as Definition.LockStatement;
				s_.Expression = ConvertExpression(s_.Expression);
				s_.Statement = ConvertStatement(s_.Statement);
				return s_;
			}

			throw new Exception();
		}

		Definition.Expression ConvertExpression(Definition.Expression e)
		{
			if (e == null)
			{
				return e;
			}
			else if (e is Definition.MemberAccessExpression)
			{
				var e_ = e as Definition.MemberAccessExpression;
				e_.Expression = ConvertExpression(e_.Expression);
				return e_;
			}
			else if(e is Definition.GenericMemberAccessExpression)
			{
				var e_ = e as Definition.GenericMemberAccessExpression;
				e_.Types = e_.Types.Select(_ => ConvertType(_)).ToArray();
				return e_;
			}
			else if (e is Definition.CastExpression)
			{
				var e_ = e as Definition.CastExpression;
				e_.Type = ConvertType(e_.Type);
				e_.Expression = ConvertExpression(e_.Expression);
				return e_;
			}
			else if (e is Definition.LiteralExpression)
			{
				return e;
			}
			else if (e is Definition.InvocationExpression)
			{
				var e_ = e as Definition.InvocationExpression;
				e_.Method = ConvertExpression(e_.Method);
				e_.Args = e_.Args.Select(_ => ConvertExpression(_)).ToArray();
				return e_;
			}
			else if (e is Definition.ObjectCreationExpression)
			{
				var e_ = e as Definition.ObjectCreationExpression;
				e_.Type = ConvertType(e_.Type);
				e_.Args = e_.Args.Select(_ => ConvertExpression(_)).ToArray();
				return e_;
			}
			else if (e is Definition.AssignmentExpression)
			{
				var e_ = e as Definition.AssignmentExpression;
				e_.Target = ConvertExpression(e_.Target);
				e_.Expression = ConvertExpression(e_.Expression);
				return e_;
			}
			else if (e is Definition.ElementAccessExpression)
			{
				var e_ = e as Definition.ElementAccessExpression;
				e_.Value = ConvertExpression(e_.Value);
				e_.Arg = ConvertExpression(e_.Arg);
				return e_;
			}
			else if (e is Definition.ThisExpression)
			{
				return e;
			}
			else if (e is Definition.IdentifierNameExpression)
			{
				var e_ = e as Definition.IdentifierNameExpression;
				return e_;
			}
			else if (e is Definition.BinaryExpression)
			{
				var e_ = e as Definition.BinaryExpression;
				e_.Left = ConvertExpression(e_.Left);
				e_.Right = ConvertExpression(e_.Right);
				return e_;
			}
			else if (e is Definition.PrefixUnaryExpression)
			{
				var e_ = e as Definition.PrefixUnaryExpression;
				e_.Expression = ConvertExpression(e_.Expression);
				return e_;
			}
			else if (e is Definition.PostfixUnaryExpression)
			{
				var e_ = e as Definition.PostfixUnaryExpression;
				e_.Operand = ConvertExpression(e_.Operand);
				return e_;
			}
			else if( e is Definition.BaseExpression)
			{
				return e;
			}
			else if (e is Definition.ObjectArrayCreationExpression)
			{
				var e_ = e as Definition.ObjectArrayCreationExpression;
				e_.Type = ConvertType(e_.Type);
				e_.Args = e_.Args.Select(_ => ConvertExpression(_)).ToArray();
				return e_;
			}
			else if (e is Definition.TypeExpression)
			{
				var e_ = e as Definition.TypeExpression;
				e_.Type = ConvertType(e_.Type);
				return e_;
			}

			throw new Exception();
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
				dst.InnerType = src.InnerType.Select(_ => ConvertType(_)).OfType<Definition.TypeSpecifier>().ToList();

				return dst;
			}

			return typeSpecifier;
		}

		
	}

}

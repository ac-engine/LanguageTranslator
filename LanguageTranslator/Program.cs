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
			var langType = "java";
			var dlls = new List<string>();

			if (args.Length >= 2)
			{
				csharpDir = args[0];
				dstDir = args[1];

				if (args.Length >= 3)
				{
					langType = args[2];

					foreach (var a in args.Skip(3))
					{
						dlls.Add(a);
					}
				}
			}

			// dst編集
			if (dstDir.Last() != '/' || dstDir.Last() != '\\')
			{
				dstDir += "/";
			}

			var parser = new Parser.Parser();
			var cs = Directory.EnumerateFiles(csharpDir, "*.cs", SearchOption.AllDirectories).ToArray();

			parser.TypesWhoseMemberNotParsed.Add("asd.Particular.GC");
			parser.TypesWhoseMemberNotParsed.Add("asd.Particular.Helper");
			parser.TypesWhoseMemberNotParsed.Add("asd.Particular.Dictionary");
			parser.TypesWhoseMemberNotParsed.Add("asd.Particular.SortedList");
			parser.TypesWhoseMemberNotParsed.Add("asd.Particular.Lambda");
			parser.TypesWhoseMemberNotParsed.Add("asd.Particular.Define");
			parser.TypesWhoseMemberNotParsed.Add("asd.Particular.WeakReference");

			Definition.Definitions definitions = null;

			try
			{
				definitions = parser.Parse(cs, dlls.ToArray());
			}
			catch (Parser.ParseException e)
			{
				Console.WriteLine(e.Message);
				return;
			}

			// コードコメントxmlの解析
			var xmlPath = string.Empty;

			if (System.IO.File.Exists(xmlPath))
			{
				var codeCommentParser = new CodeCommentParser.Parser();
				codeCommentParser.Parse(xmlPath, definitions);
			}

			// 色のみ強制変換
			var color_ = definitions.Structs.FirstOrDefault(_ => _.Name == "Color");
			if(color_ != null)
			{
				color_.Fields[0].Type = new Definition.SimpleType() { Namespace = "System", TypeName = "Int16" };
				color_.Fields[1].Type = new Definition.SimpleType() { Namespace = "System", TypeName = "Int16" };
				color_.Fields[2].Type = new Definition.SimpleType() { Namespace = "System", TypeName = "Int16" };
				color_.Fields[3].Type = new Definition.SimpleType() { Namespace = "System", TypeName = "Int16" };
			}
	

			Translator.Editor editor = new Translator.Editor(definitions);

			editor.AddMethodConverter("System.Collections.Generic", "List", "Add", "add");
			editor.AddMethodConverter("System.Collections.Generic", "List", "Remove", "remove");
			editor.AddMethodConverter("System.Collections.Generic", "List", "Clear", "clear");

			editor.AddMethodConverter("System.Collections.Generic", "LinkedList", "AddLast", "add");
			editor.AddMethodConverter("System.Collections.Generic", "LinkedList", "Remove", "remove");
			editor.AddMethodConverter("System.Collections.Generic", "LinkedList", "Contains", "contains");
			editor.AddMethodConverter("System.Collections.Generic", "LinkedList", "Clear", "clear");

			editor.AddMethodConverter("System.Collections.Generic", "Queue", "Enqueue", "add");
			editor.AddMethodConverter("System.Collections.Generic", "Queue", "Dequeue", "pop");

			editor.AddMethodConverter("System.Collections.Generic", "Dictionary", "Add", "put");
			editor.AddMethodConverter("System.Collections.Generic", "Dictionary", "ContainsKey", "containsKey");
			editor.AddMethodConverter("System.Collections.Generic", "Dictionary", "Remove", "remove");
			editor.AddMethodConverter("System.Collections.Generic", "Dictionary", "Clear", "clear");

			editor.AddMethodConverter("System.Collections.Generic", "SortedList", "ContainsKey", "containsKey");

			editor.AddMethodConverter("System", "Console", "WriteLine", "println");

			editor.AddMethodConverter("System", "String", "Substring", "substring");

			editor.AddMethodConverter("System", "Math", "Sqrt", "sqrt");
			editor.AddMethodConverter("System", "Math", "Sin", "sin");
			editor.AddMethodConverter("System", "Math", "Cos", "cos");
			editor.AddMethodConverter("System", "Math", "Atan2", "atan2");
			editor.AddMethodConverter("System", "Math", "Tan", "tan");
			editor.AddMethodConverter("System", "Math", "Exp", "exp");
			editor.AddMethodConverter("System", "Math", "Max", "max");
			editor.AddMethodConverter("System", "Math", "Min", "min");

			editor.AddTypeConverter("System", "Void", "", "void");
			editor.AddTypeConverter("System", "Boolean", "", "boolean");
			editor.AddTypeConverter("System", "Int32", "", "int");
			editor.AddTypeConverter("System", "Single", "", "float");
			editor.AddTypeConverter("System", "Double", "", "double");
			editor.AddTypeConverter("System", "Int16", "", "short");
			editor.AddTypeConverter("System", "Byte", "", "byte");

			editor.AddTypeConverter("System", "Object", "java.lang", "Object");

			editor.AddTypeConverter("System", "IntPtr", "", "long");

			editor.AddTypeConverter("System", "String", "java.lang", "String");

			editor.AddTypeConverter("System", "IDisposable", "asd.Particular.Java", "IDisposable");

			editor.AddTypeConverter("System.Collections.Generic", "List", "java.util", "ArrayList");
			editor.AddTypeConverter("System.Collections.Generic", "LinkedList", "java.util", "LinkedList");
			editor.AddTypeConverter("System.Collections.Generic", "Queue", "java.util", "LinkedList");

			editor.AddTypeConverter("System.Collections.Generic", "Dictionary", "java.util", "HashMap");
			editor.AddTypeConverter("System.Collections.Generic", "SortedList", "java.util", "TreeMap");
			editor.AddTypeConverter("System.Collections.Generic", "KeyValuePair", "java.util", "Map.Entry");
			editor.AddTypeConverter("System.Collections.Generic", "IEnumerable", "java.lang", "Iterable");

			editor.AddTypeConverter("System", "Math", "java.lang", "Math");

			editor.AddTypeConverter("System", "WeakReference", "java.lang.ref", "WeakReference");

			editor.AddTypeConverter("System", "IComparable", "java.lang", "Comparable");

			editor.AddTypeConverter("System", "Console", "System", "out");

			editor.AddIgnoredType("asd.Particular", "Dictionary");
			editor.AddIgnoredType("asd.Particular", "SortedList");
			editor.AddIgnoredType("asd.Particular", "WeakReference");
			editor.AddIgnoredType("asd.Particular", "GC");
			editor.AddIgnoredType("asd.Particular", "Helper");
			editor.AddIgnoredType("asd.Particular", "Lambda");
			editor.AddIgnoredType("asd.Particular", "Define");
			editor.AddIgnoredType("asd.Particular", "ChildDrawingMode");
			editor.AddIgnoredType("asd.Particular", "ChildManagementMode");

			{
				var def = definitions.Classes.FirstOrDefault(_ => _.Name == "Engine");

				if (def != null)
				{
					def.UserCode = @"
	public static void ChangeScene(asd.Scene scene)
	{
		ChangeScene(scene, true);
	}

	public static void ChangeSceneWithTransition(asd.Scene scene, asd.Transition transition)
	{
		ChangeSceneWithTransition(scene, transition, true);
	}
";
				}
			}

			{
				var def = definitions.Structs.FirstOrDefault(_ => _.Name == "Color");

				if (def != null)
				{
					def.UserCode = @"
	public Color(int r, int g, int b, int a) {
		R = (short)r;
		G = (short)g;
		B = (short)b;
		A = (short)a;
	}
	public Color(int r, int g, int b) {
		R = (short)r;
		G = (short)g;
		B = (short)b;
		A = 255;
	}
";
				}
			}

			{
				var def = definitions.Structs.FirstOrDefault(_ => _.Name == "FCurveKeyframe");

				if (def != null)
				{
					def.UserCode = @"

	public FCurveKeyframe(float KeyValue_X, float KeyValue_Y, float LeftHandle_X, float LeftHandle_Y, float RightHandle_X, float RightHandle_Y, int interpolationType)
	{

		KeyValue.X = KeyValue_X;
		KeyValue.Y = KeyValue_Y;
		LeftHandle.X = LeftHandle_X;
		LeftHandle.Y = LeftHandle_Y;
		RightHandle.X = RightHandle_X;
		RightHandle.Y = RightHandle_Y;
		Interpolation = Interpolation.swigToEnum(interpolationType);
	}

";
				}
			}

			{
				var def = definitions.Structs.FirstOrDefault(_ => _.Name == "Matrix33");

				if (def != null)
				{
					def.UserCode = @"

	public Matrix33(float m00, float m01, float m02,
			float m10, float m11, float m12,
			float m20, float m21, float m22)
	{
		Values[0+0*3] = m00;
		Values[1+0*3] = m01;
		Values[2+0*3] = m02;

		Values[0+1*3] = m10;
		Values[1+1*3] = m11;
		Values[2+1*3] = m12;

		Values[0+2*3] = m20;
		Values[1+2*3] = m21;
		Values[2+2*3] = m22;
	}

";
				}
			}

			{
				var def = definitions.Structs.FirstOrDefault(_ => _.Name == "Matrix44");

				if (def != null)
				{
					def.UserCode = @"

	public Matrix44(float m00, float m01, float m02, float m03,
			float m10, float m11, float m12, float m13,
			float m20, float m21, float m22, float m23,
			float m30, float m31, float m32, float m33)
	{
		Values[0+0*4] = m00;
		Values[1+0*4] = m01;
		Values[2+0*4] = m02;
		Values[3+0*4] = m03;

		Values[0+1*4] = m10;
		Values[1+1*4] = m11;
		Values[2+1*4] = m12;
		Values[3+1*4] = m13;

		Values[0+2*4] = m20;
		Values[1+2*4] = m21;
		Values[2+2*4] = m22;
		Values[3+2*4] = m23;

		Values[0+3*4] = m30;
		Values[1+3*4] = m31;
		Values[2+3*4] = m32;
		Values[3+3*4] = m33;
	}

";
				}
			}

			{
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var be = o as Definition.BinaryExpression;

					var leftType = be?.Left?.SelfType as Definition.SimpleType;
					var rightType = be?.Right?.SelfType as Definition.SimpleType;

					if (leftType == null || rightType == null) return Tuple.Create<bool, object>(true, null);

					if (leftType.TypeName == "Vector2DF" && rightType.TypeName == "Vector2DF" && be.Operator == Definition.BinaryExpression.OperatorType.Add)
					{
						// getter差し替え
						var invocation = new Definition.InvocationExpression();

						// 関数設定
						var memf = new Definition.MemberAccessExpression();
						memf.Method = new Definition.MethodDef();
						memf.Method.Name = "Add";
						memf.Method.IsStatic = true;

						memf.Struct = new Definition.StructDef();
						memf.Struct.Name = "Vector2DF";
						memf.Struct.Namespace = "asd";

						invocation.Method = memf;

						// 引数設定
						invocation.Args = new[] { be.Left, be.Right };

						return Tuple.Create<bool, object>(true, invocation);
					}

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}

			{
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var be = o as Definition.BinaryExpression;
	
					var leftType = be?.Left?.SelfType as Definition.SimpleType;
					var rightType = be?.Right?.SelfType as Definition.SimpleType;

					if (leftType == null || rightType == null) return Tuple.Create<bool, object>(true, null);

					if(leftType.TypeName == "Vector2DF" && rightType.TypeName == "Vector2DF" && be.Operator == Definition.BinaryExpression.OperatorType.Divide)
					{
						// getter差し替え
						var invocation = new Definition.InvocationExpression();

						// 関数設定
						var memf = new Definition.MemberAccessExpression();
						memf.Method = new Definition.MethodDef();
						memf.Method.Name = "Divide";
						memf.Method.IsStatic = true;

						memf.Struct = new Definition.StructDef();
						memf.Struct.Name = "Vector2DF";
						memf.Struct.Namespace = "asd";

						invocation.Method = memf;

						// 引数設定
						invocation.Args = new[] { be.Left, be.Right };

						return Tuple.Create<bool, object>(true, invocation);
					}

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}

			{
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var be = o as Definition.BinaryExpression;

					var leftType = be?.Left?.SelfType as Definition.SimpleType;
					var rightType = be?.Right?.SelfType as Definition.SimpleType;

					if (leftType == null || rightType == null) return Tuple.Create<bool, object>(true, null);

					if (leftType.TypeName == "Vector2DF" && rightType.TypeName == "Single" && be.Operator == Definition.BinaryExpression.OperatorType.Divide)
					{
						// getter差し替え
						var invocation = new Definition.InvocationExpression();

						// 関数設定
						var memf = new Definition.MemberAccessExpression();
						memf.Method = new Definition.MethodDef();
						memf.Method.Name = "DivideByScalar";
						memf.Method.IsStatic = true;

						memf.Struct = new Definition.StructDef();
						memf.Struct.Name = "Vector2DF";
						memf.Struct.Namespace = "asd";

						invocation.Method = memf;

						// 引数設定
						invocation.Args = new[] { be.Left, be.Right };

						return Tuple.Create<bool, object>(true, invocation);
					}

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}

			{
				editor.AddEditFuncPropToMethodConverter("System", "String", "Length", "length");
			}

			{
				// Listの[]差し替え
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var eae = o as Definition.ElementAccessExpression;
					var gt = eae?.Value?.SelfType as Definition.GenericType;
					if (gt?.OuterType.TypeName == "List")
					{
						// getter差し替え
						var invocation = new Definition.InvocationExpression();

						// 関数設定
						var memf = new Definition.MemberAccessExpression();
						memf.Method = new Definition.MethodDef();
						memf.Method.Name = "get";
						memf.Expression = eae.Value;
						invocation.Method = memf;

						// 引数設定
						invocation.Args = new[] { eae.Arg };

						return Tuple.Create<bool, object>(true, invocation);

					}

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}

			{
				// ListのCount差し替え
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var mae = o as Definition.MemberAccessExpression;

					if (mae != null && mae.Property != null && mae.Property.Name == "Count" && mae.Class != null &&
						(mae.Class.Name == "List" || mae.Class.Name == "Queue"))
					{
						// getter差し替え
						var invocation = new Definition.InvocationExpression();

						// 関数設定
						var memf = new Definition.MemberAccessExpression();
						memf.Method = new Definition.MethodDef();
						memf.Method.Name = "size";
						memf.Expression = mae.Expression;
						invocation.Method = memf;

						// 引数設定
						invocation.Args = new Definition.Expression[0];

						return Tuple.Create<bool, object>(true, invocation);

					}

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}

			{
				// ListのCount差し替え
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var mae = o as Definition.MemberAccessExpression;

					if (mae != null && mae.Property != null && mae.Property.Name == "Values" && mae.Class != null && mae.Class.Name == "Dictionary")
					{
						// getter差し替え
						var invocation = new Definition.InvocationExpression();

						// 関数設定
						var memf = new Definition.MemberAccessExpression();
						memf.Method = new Definition.MethodDef();
						memf.Method.Name = "values";
						memf.Expression = mae.Expression;
						invocation.Method = memf;

						// 引数設定
						invocation.Args = new Definition.Expression[0];

						return Tuple.Create<bool, object>(true, invocation);

					}

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}
			
			{
				// 代入のプロパティ差し替え
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var ae = o as Definition.AssignmentExpression;

					if (ae != null)
					{
						var mae = ae.Target as Definition.MemberAccessExpression;
						if (mae != null && mae.IsProperty)
						{
							// setter差し替え
							var invocation = new Definition.InvocationExpression();

							// 関数設定
							var memf = new Definition.MemberAccessExpression();
							memf.Method = new Definition.MethodDef();
							memf.Method.Name = "set" + (mae.Property != null ? mae.Property.Name : mae.Name);
							memf.Expression = mae.Expression;

							invocation.Method = memf;

							// 引数設定
							invocation.Args = new[] { ae.Expression };

							return Tuple.Create<bool, object>(false, invocation);
						}

						var ime = ae.Target as Definition.IdentifierNameExpression;
						if (ime != null && ime.IsProperty)
						{
							// setter差し替え
							var invocation = new Definition.InvocationExpression();

							// 関数設定
							var memf = new Definition.MemberAccessExpression();
							memf.Method = new Definition.MethodDef();
							memf.Method.Name = "set" + ime.Name;

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
				// メンバーアクセスのプロパティ差し替え
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var mae = o as Definition.MemberAccessExpression;

					if (mae?.Property != null)
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

			// enumの処理
			{
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					{
						var ce = o as Definition.CastExpression;
						if (ce != null)
						{
							var st = ce.Type as Definition.SimpleType;
							if (st != null && st.TypeKind == Definition.SimpleTypeKind.Enum)
							{
								// getter差し替え
								var castInvocation = new Definition.InvocationExpression();

								// 関数設定
								var convertF = new Definition.MemberAccessExpression();
								convertF.Method = new Definition.MethodDef();
								convertF.Method.Name = st.Namespace + "." + st.TypeName + ".swigToEnum";
								convertF.Expression = null;

								var getF = new Definition.MemberAccessExpression();
								getF.Method = new Definition.MethodDef();
								getF.Method.Name = "swigValue";
								castInvocation.Method = getF;
								getF.Expression = ce.Expression;

								var getInvocation = new Definition.InvocationExpression();
								getInvocation.Method = getF;
								getInvocation.Args = new Definition.Expression[0];

								// 引数設定
								castInvocation.Method = convertF;
								castInvocation.Args = new[] { (Definition.Expression)getInvocation };

								return Tuple.Create<bool, object>(true, castInvocation);
							}
						}
					}

					/*
					{
						var mae = o as Definition.MemberAccessExpression;
						if (mae != null && mae.EnumMember != null)
						{
							// getter差し替え
							var invocation = new Definition.InvocationExpression();

							// 関数設定
							var memf = new Definition.MemberAccessExpression();
							memf.Method = new Definition.MethodDef();
							memf.Method.Name = "getID";
							invocation.Method = memf;
							memf.Expression = mae;

							// 引数設定
							invocation.Args = new Definition.Expression[0];

							return Tuple.Create<bool, object>(false, invocation);
						}
					}

					{
						var ine = o as Definition.IdentifierNameExpression;
						if (ine != null && (ine.Type is Definition.SimpleType) && (ine.Type as Definition.SimpleType).TypeKind == Definition.SimpleTypeKind.Enum)
						{
							// getter差し替え
							var invocation = new Definition.InvocationExpression();

							// 関数設定
							var memf = new Definition.MemberAccessExpression();
							memf.Method = new Definition.MethodDef();
							memf.Method.Name = "getID";
							invocation.Method = memf;
							memf.Expression = ine;

							// 引数設定
							invocation.Args = new Definition.Expression[0];

							return Tuple.Create<bool, object>(false, invocation);
						}
					}
					*/

					return Tuple.Create<bool, object>(true, null);
				};

				editor.AddEditFunc(func);
			}

			// ジェネリックメソッド
			{
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var ive = o as Definition.InvocationExpression;

					if (ive != null)
					{
						var gne = ive.Method as Definition.GenericNameExpression;
						if (gne != null)
						{
							var mae = new Definition.MemberAccessExpression();
							mae.Name = gne.Name;
							mae.Types = gne.Types;
							mae.Expression = new Definition.ThisExpression();
							ive.Method = mae;
							return Tuple.Create<bool, object>(false, ive);
						}
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
					if (id != null && id.Name == "Particular")
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

			// Genericsの型変換
			{
				Func<object, Tuple<bool, object>> func = (object o) =>
				{
					var gt = o as Definition.GenericType;

					if (gt != null)
					{
						foreach (var t in gt.InnerType)
						{
							var t_ = t as Definition.SimpleType;
							if (t_ != null)
							{
								if (t_.TypeName == "Int32")
								{
									t_.Namespace = "java.lang";
									t_.TypeName = "Integer";
								}

								// byte対策
								if (t_.TypeName == "Byte")
								{
									t_.Namespace = "java.lang";
									t_.TypeName = "Byte";
								}

								if (t_.TypeName == "IntPtr")
								{
									t_.Namespace = "java.lang";
									t_.TypeName = "Long";
								}
							}
						}

						return Tuple.Create<bool, object>(true, gt);
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


}

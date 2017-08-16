using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Translator.Java
{
	class Translator : LanguageTranslator.Translator.Translator
	{
		private const string PackageName = "asd";
		
		void Summary(Definition.SummaryComment summary)
		{
			if (string.IsNullOrEmpty(summary.Summary)) return;

			WriteLine("/**");
			WriteLine(" * {0}", summary.Summary);

			foreach(var p in summary.ParamComments)
			{
				WriteLine(" * @param {0} {1}", p.Name, p.Comment);
			}

			WriteLine("*/");
		}

		string GetGenericsTypeParameters(List<Definition.TypeParameterDef> typeParameters)
		{
			if (typeParameters.Count == 0) return string.Empty;

			Func<Definition.TypeParameterDef, string> generic = (t) =>
			{
				if (t.BaseTypeConstraints.Count > 0)
				{
					return t.Name + " extends " + string.Join(" & ", t.BaseTypeConstraints.Select(_ => GetTypeSpecifier(_)));
				}
				else
				{
					return t.Name;
				}
			};

			return "<" + string.Join(",", typeParameters.Select(_ => generic(_))) + ">";
		}

		string GetAccessLevel(Definition.AccessLevel a)
		{
			switch (a)
			{
				case LanguageTranslator.Definition.AccessLevel.Public:
					return "public";
				case LanguageTranslator.Definition.AccessLevel.Protected:
					return "protected";
				case LanguageTranslator.Definition.AccessLevel.Private:
					return "private";
				case LanguageTranslator.Definition.AccessLevel.Internal:
					return "";
				case LanguageTranslator.Definition.AccessLevel.ProtectedInternal:
					return "protected";
				default:
					throw new NotImplementedException("unknown access modifier " + Enum.GetName(a.GetType(), a));
			}
		}

		string GetTypeSpecifier(Definition.TypeSpecifier t)
		{
			if (t is Definition.SimpleType)
			{
				var t2 = (Definition.SimpleType)t;
				return string.Format("{0}{1}", t2.Namespace == null || t2.Namespace == "" ? "" : t2.Namespace + ".", t2.TypeName);
			}
			else if (t is Definition.ArrayType)
			{
				var t2 = (Definition.ArrayType)t;
				return string.Format("{0}[]", GetTypeSpecifier(t2.BaseType));
			}
			else if (t is Definition.GenericType)
			{
				var t2 = (Definition.GenericType)t;
				return string.Format("{0}<{1}>", GetTypeSpecifier(t2.OuterType), string.Join(", ", t2.InnerType.ConvertAll(GetTypeSpecifier)));
			}
			else if (t is Definition.NullableType)
			{
				var t2 = (Definition.NullableType)t;
				return string.Format("Optional<{0}>", GetTypeSpecifier(t2.BaseType));
			}
			else if (t is Definition.GenericTypenameType)
			{
				var t2 = (Definition.GenericTypenameType)t;
				return t2.Name;
			}
			else
			{
				throw new NotImplementedException("unknown type " + Enum.GetName(t.GetType(), t));
			}
		}

		string GetExpression(Definition.Expression e)
		{
			if (e == null) { return ""; }
			if (e is Definition.BinaryExpression)
			{
				var e2 = (Definition.BinaryExpression)e;

				// as 対応
				if (e2.Operator == Definition.BinaryExpression.OperatorType.As)
				{
					return string.Format("({0} instanceof {1}? ({1}){0}: null)", GetExpression(e2.Left), GetExpression(e2.Right));
				}

				// 文字列の比較対応
				if(
					(e2.Left.SelfType as Definition.SimpleType)?.TypeName == "String" &&
					(e2.Right.SelfType as Definition.SimpleType)?.TypeName == "String" &&
					e2.Operator == Definition.BinaryExpression.OperatorType.Equals)
				{
					return string.Format("({0}.equals({1}))", GetExpression(e2.Left), GetExpression(e2.Right));
				}


				return string.Format("({0} {1} {2})", GetExpression(e2.Left), GetBinaryExpressionOperator(e2.Operator), GetExpression(e2.Right));
			}
			else if (e is Definition.AssignmentExpression)
			{
				var e2 = (Definition.AssignmentExpression)e;

				if (e2.Type == Definition.AssignmentExpression.OperatorType.Simple)
				{
					return string.Format("{0} = {1}", GetExpression(e2.Target), GetExpression(e2.Expression));
				}
				else if (e2.Type == Definition.AssignmentExpression.OperatorType.Add)
				{
					return string.Format("{0} += {1}", GetExpression(e2.Target), GetExpression(e2.Expression));
				}
				else if (e2.Type == Definition.AssignmentExpression.OperatorType.Substract)
				{
					return string.Format("{0} -= {1}", GetExpression(e2.Target), GetExpression(e2.Expression));
				}
				else if (e2.Type == Definition.AssignmentExpression.OperatorType.Divide)
				{
					return string.Format("{0} /= {1}", GetExpression(e2.Target), GetExpression(e2.Expression));
				}
				else if (e2.Type == Definition.AssignmentExpression.OperatorType.Modulo)
				{
					return string.Format("{0} %= {1}", GetExpression(e2.Target), GetExpression(e2.Expression));
				}
				else
				{
					throw new Exception();
				}
			}
			else if (e is Definition.CastExpression)
			{
				var e2 = (Definition.CastExpression)e;
				return string.Format("({0}){1}", GetTypeSpecifier(e2.Type), GetExpression(e2.Expression));

			}
			else if (e is Definition.ElementAccessExpression)
			{
				var e2 = (Definition.ElementAccessExpression)e;
				return string.Format("{0}[{1}]", GetExpression(e2.Value), GetExpression(e2.Arg));
			}
			else if (e is Definition.IdentifierNameExpression)
			{
				var e2 = (Definition.IdentifierNameExpression)e;
				return e2.Name;
			}
			else if (e is Definition.InvocationExpression)
			{
				var e2 = (Definition.InvocationExpression)e;
				return string.Format("{0}({1})", GetExpression(e2.Method), string.Join(", ", Array.ConvertAll(e2.Args, GetExpression)));
			}
			else if (e is Definition.LiteralExpression)
			{
				var e2 = (Definition.LiteralExpression)e;
				return ParseLiteralExpression(e2);				
			}
			else if (e is Definition.MemberAccessExpression)
			{
				var e2 = (Definition.MemberAccessExpression)e;

				var accessed = GetExpression(e2.Expression);
				if(accessed != string.Empty)
				{
					accessed += ".";
				}

				var generic = string.Empty;
				if(e2.Types.Count() > 0)
				{
					generic = string.Format("<{0}>", string.Join(",", e2.Types.Select(_ => GetTypeSpecifier(_))));
				}

				if (e2.EnumMember != null)
				{
					return string.Format("{0}{1}.{2}", (e2.Enum.Namespace ==null || e2.Enum.Namespace == "") ? "": e2.Enum.Namespace + "." , e2.Enum.Name, e2.EnumMember.Name);
				}
				else if (e2.Method != null)
				{
					if(e2.Method.IsStatic && e2.Class != null)
					{
						var fullname = string.Empty;
						if(e2.Class.Namespace != "")
						{
							fullname = e2.Class.Namespace + "." + e2.Class.Name;
						}
						else
						{
							fullname = e2.Class.Name;
						}

						return string.Format("{0}{1}{2}{3}", accessed, fullname + ".", generic, e2.Method.Name);
					}
					else if (e2.Method.IsStatic && e2.Struct != null)
					{
						var fullname = string.Empty;
						if (e2.Struct.Namespace != "")
						{
							fullname = e2.Struct.Namespace + "." + e2.Struct.Name;
						}
						else
						{
							fullname = e2.Struct.Name;
						}

						return string.Format("{0}{2}{1}", accessed, fullname + "." + e2.Method.Name, generic);
					}
					else
					{
						return string.Format("{0}{2}{1}", accessed, e2.Method.Name, generic);
					}
				}
				else if (e2.Property != null)
				{
					return string.Format("{0}{2}{1}", accessed, e2.Property.Name, generic);
				}
				else if (e2.Expression != null)
				{
					return string.Format("{0}{2}{1}", accessed, e2.Name, generic);
				}
				else
				{
					return e2.Name;
				}

			}
			else if (e is Definition.ObjectCreationExpression)
			{
				var e2 = (Definition.ObjectCreationExpression)e;
				return string.Format("new {0}({1})", GetTypeSpecifier(e2.Type), string.Join(", ", Array.ConvertAll(e2.Args, GetExpression)));
			}
			else if (e is Definition.PrefixUnaryExpression)
			{
				var e2 = (Definition.PrefixUnaryExpression)e;
				return string.Format("{0}{1}", GetPrefixUnaryExpressionOperator(e2.Type), GetExpression(e2.Expression));
			}
			else if (e is Definition.PostfixUnaryExpression)
			{
				var e2 = (Definition.PostfixUnaryExpression)e;
				return string.Format("{0}{1}", GetExpression(e2.Operand), GetPostfixUnaryExpressionOperator(e2.Type));
			}
			else if (e is Definition.ThisExpression)
			{
				return "this";
			}
			else if (e is Definition.BaseExpression)
			{
				return "super";
			}
			else if (e is Definition.ObjectCreationExpression)
			{
				var e2 = (Definition.ObjectCreationExpression)e;
				var args = e2.Args.Select(_ => GetExpression(_));

				return string.Format("new {0}({1})", GetTypeSpecifier(e2.Type), string.Join(",", args));

			}
			else if (e is Definition.ObjectArrayCreationExpression)
			{
				var e2 = (Definition.ObjectArrayCreationExpression)e;
				var args = e2.Args.Select(_ => GetExpression(_));

				return string.Format("new {0}[{1}]", GetTypeSpecifier(e2.Type), string.Join(",", args));
			}
			else if (e is Definition.GenericNameExpression)
			{
				var e2 = (Definition.GenericNameExpression)e;

				if(e2.IsMethod)
				{
					return string.Format("<{1}>{0}", e2.Name, string.Join(",", Array.ConvertAll(e2.Types, GetTypeSpecifier)));
				}
				else
				{
					return string.Format("{0}<{1}>", e2.Name, string.Join(",", Array.ConvertAll(e2.Types, GetTypeSpecifier)));
				}
			}
			else if (e is Definition.TypeExpression)
			{
				var e2 = (Definition.TypeExpression)e;
				return string.Format("{0}", GetTypeSpecifier(e2.SelfType));
			}
			else if (e is Definition.InitializerExpression)
			{
				var e2 = (Definition.InitializerExpression)e;
				return "{\n" + string.Format("{0}", string.Join(",\n", Array.ConvertAll(e2.Expressions, GetExpression))) + "}";
			}
			else
			{
				throw new NotImplementedException("unknown expression " + e.GetType().ToString());
			}
		}

		private void OutputStatements(ICollection<Definition.Statement> statements)
		{
			int preLine = 0;
			foreach (var e in statements)
			{
				for (int i = 0; i < e.StartingLine - preLine - 1; i++)
				{
					WriteLine();
				}

				OutputStatement(e);

				preLine = e.EndingLine;
			}
		}

		private void OutputStatement(Definition.Statement s)
		{
			if (s == null)
			{
				WriteLine("/* debug: null statement */");
			}
			else if (s is Definition.BlockStatement)
			{
				var s2 = (Definition.BlockStatement)s;
				OutputStatements(s2.Statements);
			}
			else if (s is Definition.ContinueStatement)
			{
				WriteLine("continue;");
			}
			else if (s is Definition.BreakStatement)
			{
				WriteLine("break;");
			}
			else if (s is Definition.ExpressionStatement)
			{
				var s2 = (Definition.ExpressionStatement)s;
				WriteLine("{0};", GetExpression(s2.Expression));
			}
			else if (s is Definition.ForeachStatement)
			{
				var s2 = (Definition.ForeachStatement)s;
				WriteLine("for({0} {1}: {2})", GetTypeSpecifier(s2.Type), s2.Name, GetExpression(s2.Value));
				WriteLine("{{");
				IndentDepth++;
				OutputStatement(s2.Statement);
				IndentDepth--;
				WriteLine("}}");
			}
			else if (s is Definition.ForStatement)
			{
				var s2 = (Definition.ForStatement)s;
				WriteLine("for({0} {1} = {2}; {3}; {4})", GetTypeSpecifier(s2.Declaration.Type), s2.Declaration.Name, GetExpression(s2.Declaration.Value), GetExpression(s2.Condition), GetExpression(s2.Incrementor));
				WriteLine("{{");
				IndentDepth++;
				OutputStatement(s2.Statement);
				IndentDepth--;
				WriteLine("}}");
			}
			else if (s is Definition.WhileStatement)
			{
				var s2 = (Definition.WhileStatement)s;
				WriteLine("while({0})", GetExpression(s2.Condition));
				WriteLine("{{");

				IndentDepth++;
				OutputStatement(s2.Statement);
				IndentDepth--;

				WriteLine("}}");
				WriteLine("");
			}
			else if (s is Definition.IfStatement)
			{
				var s2 = (Definition.IfStatement)s;
				WriteLine("if({0})", GetExpression(s2.Condition));
				WriteLine("{{");

				IndentDepth++;
				OutputStatement(s2.TrueStatement);
				IndentDepth--;

				if (s2.FalseStatement != null)
				{
					WriteLine("}}");
					WriteLine("else");
					WriteLine("{{");

					IndentDepth++;
					OutputStatement(s2.FalseStatement);
					IndentDepth--;

					WriteLine("}}");
				}
				else
				{
					WriteLine("}}");
				}
			}
			else if (s is Definition.ReturnStatement)
			{
				var s2 = (Definition.ReturnStatement)s;
				WriteLine("return {0};", GetExpression(s2.Return));
			}
			else if (s is Definition.VariableDeclarationStatement)
			{
				MakeIndent();
				var s2 = (Definition.VariableDeclarationStatement)s;
				Res.AppendFormat("{0} {1}", GetTypeSpecifier(s2.Type), s2.Name);
				if (s2.Value != null)
				{
					Res.AppendFormat(" = {0};\r\n", GetExpression(s2.Value));
				}
				else
				{
					Res.AppendLine(";");
				}
			}
			else if (s is Definition.LockStatement)
			{

				var s2 = (Definition.LockStatement)s;
				
				WriteLine("synchronized({0})", GetExpression(s2.Expression));
				WriteLine("{{");

				IndentDepth++;
				OutputStatement(s2.Statement);
				IndentDepth--;

				WriteLine("}}");
			}
			else if (s is Definition.CommentStatement)
			{
				var s2 = (Definition.CommentStatement)s;
				WriteLine("// {0}", s2.Text);
			}
			else
			{
				throw new NotImplementedException("unknown statement " + s.GetType().ToString());
			}

		}


		private string GetParamStr(List<Definition.ParameterDef> ps)
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
				res.Append(GetTypeSpecifier(p.Type));
				res.Append(" ");
				res.Append(p.Name);
			}
			return res.ToString();
		}

		private void OutputField(Definition.FieldDef f)
		{
			//Summary(f.Brief);
			MakeIndent();

			if(f.Argument == null)
			{
				Res.AppendFormat("{2} {3}{0} {1}", GetTypeSpecifier(f.Type), f.Name, GetAccessLevel(f.AccessLevel), f.IsStatic ? "static " : "");

				if (f.Initializer != null)
				{
					Res.AppendFormat(" = {0};\r\n", GetExpression(f.Initializer));
				}
				else
				{
					Res.AppendLine(";");
				}
			}
			else
			{
				// 無理やりfixedArrayを再現
				var atype = (Definition.ArrayType)f.Type;

				Res.AppendFormat("{0} {1}{2} {3} = new {4}[{5}];\r\n",
					GetAccessLevel(f.AccessLevel),
					f.IsStatic ? "static " : "",
					GetTypeSpecifier(f.Type), f.Name,
					GetTypeSpecifier(atype.BaseType),
					f.Argument);
			}
		}

		private void OutputFieldInInterface(Definition.FieldDef f)
		{
			//Summary(f.Brief);
			MakeIndent();
			Res.AppendFormat("{0} {1};", GetTypeSpecifier(f.Type), f.Name);
		}

		private void OutputConstructor(string name, Definition.ConstructorDef c)
		{
			//Summary(c.Brief);
			
			WriteLine("{0} {1}({2})", GetAccessLevel(c.AccessLevel), name, GetParamStr(c.Parameters));
			WriteLine("{{");

			IndentDepth++;
			if (c.Initializer != null)
			{
				MakeIndent();

				var tob = c.Initializer.ThisOrBase;
				if(c.Initializer.ThisOrBase == "base")
				{
					tob = "super";
				}
				Res.AppendFormat("{0}({1});\r\n", tob, string.Join(", ", c.Initializer.Arguments.ConvertAll(GetExpression)));
			}

			OutputStatements(c.Body);

			IndentDepth--;
			WriteLine("}}");
		}

		private void OutputMethod(Definition.MethodDef m)
		{
			Func<string> generics = () =>
			{
				if (m.TypeParameters.Count == 0) return string.Empty;
				return "<" + string.Join(",", m.TypeParameters.Select(_ => _.Name).ToArray()) + ">";
			};

			Summary(m.Summary);

			List<string> strs = new List<string>();
			strs.Add(GetAccessLevel(m.AccessLevel));
			strs.Add(m.IsStatic ? "static " : "");
			strs.Add(GetGenericsTypeParameters(m.TypeParameters));
			strs.Add(m.IsAbstract ? "abstract " : "");
			strs.Add(GetTypeSpecifier(m.ReturnType));
			strs.Add(m.Name);

			var strs_ = string.Join(" ", strs.Where(_ => !string.IsNullOrEmpty(_)).ToArray());

			WriteLine("{0}({1})", strs_, GetParamStr(m.Parameters));

			if(m.IsAbstract)
			{
				Res.AppendLine(";");
			}
			else
			{
				WriteLine("{{");
				IndentDepth++;

				OutputStatements(m.Body);

				IndentDepth--;
				WriteLine("}}");
			}
		}

		private void OutputMethodInInterface(Definition.MethodDef m)
		{
			//Summary(m.);
			WriteLine("{0} {1}({2});", GetTypeSpecifier(m.ReturnType), m.Name, GetParamStr(m.Parameters));
		}


		private void OutputProperty(Definition.PropertyDef p)
		{

			var needVariable = true;

			if (p.Setter != null)
			{
				if (p.Setter.Body != null)
				{
					Summary(p.Summary);

					WriteLine("{2} {3}void set{0}({1} value) {{", p.Name, GetTypeSpecifier(p.Type), GetAccessLevel(p.AccessLevel), p.IsStatic ? "static " : "");
					IndentDepth++;
					OutputStatement(p.Setter.Body);
					IndentDepth--;
					WriteLine("}}");
					needVariable = false;
				}
				else
				{
					WriteLine("{2} {3}void set{0}({1} value) {{ {0} = value; }}", p.Name, GetTypeSpecifier(p.Type), GetAccessLevel(p.AccessLevel), p.IsStatic ? "static " : "");
				}
			}


			if (p.Getter != null)
			{
				if (p.Getter.Body != null)
				{
					Summary(p.Summary);

					WriteLine("{2} {3}{0} get{1}() {{", GetTypeSpecifier(p.Type), p.Name, GetAccessLevel(p.AccessLevel), p.IsStatic ? "static " : "");
					IndentDepth++;
					OutputStatement(p.Getter.Body);
					IndentDepth--;
					WriteLine("}}");
					needVariable = false;
				}
				else
				{
					WriteLine("{2} {3}{0} get{1}() {{ return {1}; }}", GetTypeSpecifier(p.Type), p.Name, GetAccessLevel(p.AccessLevel), p.IsStatic ? "static " : "");
				}

			}

			if (needVariable)
			{
				WriteLine("private {2} {0} {1};", GetTypeSpecifier(p.Type), p.Name, p.IsStatic ? "static " : "");
			}

		}

		private void OutputPropertyInInterface(Definition.PropertyDef p)
		{
			if (p.Setter != null)
			{
				WriteLine("void set{0}({1} value);", p.Name, GetTypeSpecifier(p.Type));
			}


			if (p.Getter != null)
			{
				WriteLine("{0} get{1}();", GetTypeSpecifier(p.Type), p.Name);
			}
		}

		private void OutputClass(Definition.ClassDef cs)
		{
			Summary(cs.Summary);
			MakeIndent();

			List<Definition.TypeSpecifier> bases = new List<Definition.TypeSpecifier>();
			List<Definition.TypeSpecifier> interfaces = new List<Definition.TypeSpecifier>();

			foreach(var b in cs.BaseTypes)
			{
				var simple = b as Definition.SimpleType;
				var gene = b as Definition.GenericType;
			
				if(simple != null)
				{
					if(simple.TypeKind == Definition.SimpleTypeKind.Interface || simple.TypeKind == Definition.SimpleTypeKind.Other)
					{
						interfaces.Add(b);
					}
					else
					{
						bases.Add(b);
					}
				}
				else if(gene != null)
				{
					if (gene.OuterType.TypeKind == Definition.SimpleTypeKind.Interface || gene.OuterType.TypeKind == Definition.SimpleTypeKind.Other)
					{
						interfaces.Add(b);
					}
					else
					{
						bases.Add(b);
					}
				}
			}

			Func<string> extends = () =>
				{
					if (bases.Count == 0) return string.Empty;
					return "extends " + string.Join(",", GetTypeSpecifier(bases[0]));
				};

			Func<string> implements = () =>
			{
				if (interfaces.Count == 0) return string.Empty;
				return "implements " + string.Join(",", interfaces.Select(_ => GetTypeSpecifier(_)));
			};

			Func<string> generics = () =>
			{
				return GetGenericsTypeParameters(cs.TypeParameters);
			};

			List<string> strs_1 = new List<string>();
			strs_1.Add(GetAccessLevel(cs.AccessLevel));
			strs_1.Add(cs.IsAbstract ? "abstract" : "");
			strs_1.Add("class");
			strs_1.Add(cs.Name);

			List<string> strs_2 = new List<string>();
			strs_2.Add(string.Join(" ", strs_1.Where(_ => !string.IsNullOrEmpty(_)).ToArray()) + generics());
			strs_2.Add(extends());
			strs_2.Add(implements());

			var strs_2_ = string.Join(" ", strs_2.Where(_ => !string.IsNullOrEmpty(_)).ToArray());

			WriteLine(strs_2_);
			WriteLine("{{");

			IndentDepth++;

			Res.Append(cs.UserCode);

			foreach (var f in cs.Fields)
			{
				OutputField(f);
			}

			foreach (var p in cs.Properties)
			{
				OutputProperty(p);
			}

			foreach (var m in cs.Methods)
			{
				OutputMethod(m);
			}

			if (cs.Constructors != null)
			{
				foreach (var c in cs.Constructors)
				{
					OutputConstructor(cs.Name, c);
				}
			}

			if (cs.Destructors != null)
			{
				foreach (var d in cs.Destructors)
				{
					WriteLine("@Override");
					WriteLine("protected void finalize() throws Throwable {{");
					IndentDepth++;
					if (cs.BaseTypes != null && cs.BaseTypes.Count > 0)
					{
						WriteLine("try {{ super.finalize(); }} finally {{");
						IndentDepth++;
					}
					foreach (var s in d.Body)
					{
						OutputStatement(s);
					}

					if (cs.BaseTypes != null)
					{
						IndentDepth--;
						WriteLine("}}");
					}

					IndentDepth--;
					WriteLine("}}");
				}
			}

			IndentDepth--;
			WriteLine("}}");
		}

		private void OutputEnum(Definition.EnumDef es)
		{
			Summary(es.Summary);
			WriteLine("public enum {0} {{", es.Name);
			IndentDepth++;

			int count = 0;
			foreach (var e in es.Members)
			{
				Summary(e.Summary);
				MakeIndent();
				Res.Append(e.Name);
				if (e.Value != null)
				{
					var expression = GetExpression(e.Value);
					var suffix = string.Empty;
					if(expression.Contains("swig"))
					{
						suffix = ".swigValue()";
					}

					if(count != es.Members.Count - 1)
					{
						Res.AppendFormat("({0}{1}),\r\n", expression, suffix);
					}
					else
					{
						Res.AppendFormat("({0}{1});\r\n", expression, suffix);
					}
				}
				else
				{
					Res.AppendLine(",");
				}

				count++;
			}

			// 定型文
			var idText = @"
	private final int id;
	
	private {0}(final int id)
	{
		this.id = id;
	}
	
	public int swigValue()
	{
		return id;
	}
	
	public static {0} swigToEnum(int id)
	{
		for ({0} e : values() )
		{
			if (e.swigValue() == id)
			{
				return e;
			}
		}
	
		throw new IllegalArgumentException(""Not found : "" + id);
	}
";
			idText = idText.Replace("{0}", es.Name);

			Res.AppendLine("");
			Res.Append(idText);
			Res.AppendLine("");

			IndentDepth--;
			WriteLine("}}");
		}


		private void OutputStruct(Definition.StructDef ss)
		{
			Summary(ss.Summary);
			WriteLine("{1} class {0}", ss.Name, GetAccessLevel(ss.AccessLevel));
			WriteLine("{{");

			IndentDepth++;

			Res.Append(ss.UserCode);

			foreach (var f in ss.Fields)
			{
				OutputField(f);
			}

			foreach (var p in ss.Properties)
			{
				OutputProperty(p);
			}

			foreach (var m in ss.Methods)
			{
				OutputMethod(m);
			}

			// デフォルトコンストラクタ
			{
				var name = ss.Name;
				var constructor = "public " + name + "() {{}}";
				WriteLine(constructor);
			}

			if (ss.Constructors != null)
			{
				foreach (var c in ss.Constructors)
				{
					OutputConstructor(ss.Name, c);
				}
			}

			if (ss.Destructors != null)
			{
				foreach (var d in ss.Destructors)
				{
					WriteLine("@Override");
					WriteLine("protected void finalize() throws Throwable {{");
					IndentDepth++;
					if (ss.BaseTypes != null && ss.BaseTypes.Count > 0)
					{
						WriteLine("try {{ super.finalize(); }} finally {{");
						IndentDepth++;
					}
					foreach (var s in d.Body)
					{
						OutputStatement(s);
					}
					if (ss.BaseTypes != null)
					{
						IndentDepth--;
						WriteLine("}}");

					}
					IndentDepth--;
					WriteLine("}}");
				}
			}

			IndentDepth--;
			WriteLine("}}");
		}

		private void OutputInterface(Definition.InterfaceDef def)
		{
			Func<string> generics = () =>
			{
				if (def.TypeParameters.Count == 0) return string.Empty;

				Func<Definition.TypeParameterDef, string> generic = (t) =>
				{
					if (t.BaseTypeConstraints.Count > 0)
					{
						return t.Name + " extends " + string.Join(" & ", t.BaseTypeConstraints.Select(_ => GetTypeSpecifier(_)));
					}
					else
					{
						return t.Name;
					}
				};

				return "<" + string.Join(",", def.TypeParameters.Select(_ => generic(_))) + ">";
			};


			List<Definition.TypeSpecifier> bases = new List<Definition.TypeSpecifier>();
			List<Definition.TypeSpecifier> interfaces = new List<Definition.TypeSpecifier>();

			foreach (var b in def.BaseTypes)
			{
				var simple = b as Definition.SimpleType;
				var gene = b as Definition.GenericType;

				if (simple != null)
				{
					if (simple.TypeKind == Definition.SimpleTypeKind.Interface || simple.TypeKind == Definition.SimpleTypeKind.Other)
					{
						interfaces.Add(b);
					}
					else
					{
						bases.Add(b);
					}
				}
				else if (gene != null)
				{
					if (gene.OuterType.TypeKind == Definition.SimpleTypeKind.Interface || gene.OuterType.TypeKind == Definition.SimpleTypeKind.Other)
					{
						interfaces.Add(b);
					}
					else
					{
						bases.Add(b);
					}
				}
			}

			Func<string> extends = () =>
			{
				if (bases.Count == 0) return string.Empty;
				return " extends " + string.Join(",", GetTypeSpecifier(bases[0]));
			};

			// 表記変化注意
			Func<string> implements = () =>
			{
				if (interfaces.Count == 0) return string.Empty;
				return " extends " + string.Join(",", interfaces.Select(_ => GetTypeSpecifier(_)));
			};


			Summary(def.Summary);
			WriteLine(
				"{0} interface {1}{2} {3} {4}",
				GetAccessLevel(def.AccessLevel),
				def.Name,
				generics(),
				extends(),
				implements());

			WriteLine("{{");

			IndentDepth++;

			if (def.Fields != null)
			{
				foreach (var f in def.Fields)
				{
					OutputFieldInInterface(f);
				}
			}

			foreach (var p in def.Properties)
			{
				OutputPropertyInInterface(p);
			}

			foreach (var m in def.Methods)
			{
				OutputMethodInInterface(m);
			}

			IndentDepth--;

			WriteLine("}}");
		}

		public override void Translate(string targetDir, Definition.Definitions definisions)
		{
			var sep = Path.DirectorySeparatorChar.ToString();

			foreach (Definition.EnumDef o in definisions.Enums)
			{
				IndentDepth = 0;
				if (o.IsDefinedBySWIG) { continue; }
				var subDir = targetDir + string.Join(sep, o.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + sep + o.Name + ".java");

				if (o.Namespace != string.Empty)
				{
					Res.AppendFormat("package {0};\r\n\r\n", o.Namespace);
				}

				OutputEnum(o);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

			foreach (var o in definisions.Classes)
			{
				IndentDepth = 0;
				if (o.IsDefinedBySWIG) { continue; }
				if (o.IsDefinedDefault) { continue; }
				if (!o.IsExported) { continue; }

				var subDir = targetDir + string.Join(sep, o.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + sep + o.Name + ".java");

				if (o.Namespace != string.Empty)
				{
					Res.AppendFormat("package {0};\r\n\r\n", o.Namespace);
				}

				OutputClass(o);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

			foreach (var o in definisions.Structs)
			{
				if (o.IsDefinedDefault) { continue; }

				IndentDepth = 0;
				var subDir = targetDir + string.Join(sep, o.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + sep + o.Name + ".java");

				if (o.Namespace != string.Empty)
				{
					Res.AppendFormat("package {0};\r\n\r\n", o.Namespace);
				}

				OutputStruct(o);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

			foreach (var o in definisions.Interfaces)
			{
				IndentDepth = 0;
				var subDir = targetDir + string.Join(sep, o.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + sep + o.Name + ".java");

				if(o.Namespace != string.Empty)
				{
					Res.AppendFormat("package {0};\r\n\r\n", o.Namespace);
				}
				
				OutputInterface(o);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

		}

		public override string GetRawLiteral(string text)
		{
			text = text.Replace("\r", "");
			var strs = text.Split('\n');
			strs = strs.Select(_=>  "\"" + _ + "\"").ToArray();

			return string.Join("+\r\n", strs);
		}
	}
}

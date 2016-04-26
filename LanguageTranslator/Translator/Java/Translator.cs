using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Translator.Java
{
	class Translator : ITranslator
	{
		private const string PackageName = "asd";
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
			Res.AppendFormat("/* {0} */\r\n", brief);
		}

		private string GetAccessLevel(Definition.AccessLevel a)
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

		private string GetBinaryExpressionOperator(Definition.BinaryExpression.OperatorType o)
		{
			switch (o)
			{
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Add:
					return "+";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Subtract:
					return "-";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Multiply:
					return "*";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Divide:
					return "/";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.LogicalAnd:
					return "&&";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.LogicalOr:
					return "||";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.GreaterThan:
					return ">";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.LessThan:
					return "<";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.LessThanOrEqual:
					return "<=";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Equals:
					return "==";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.NotEquals:
					return "!=";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Is:
					return "instanceof";
				default:
					throw new NotImplementedException("unknown operator " + Enum.GetName(o.GetType(), o));
			}
		}

		private string GetPrefixUnaryExpressionOperator(Definition.PrefixUnaryExpression.OperatorType o)
		{
			switch (o)
			{
				case Definition.PrefixUnaryExpression.OperatorType.LogicalNot:
					return "!";
				case Definition.PrefixUnaryExpression.OperatorType.UnaryPlus:
					return "+";
				case Definition.PrefixUnaryExpression.OperatorType.UnaryMinus:
					return "-";
				default:
					throw new NotImplementedException("unknown operator " + Enum.GetName(o.GetType(), o));
			}
		}
		private string GetPostfixUnaryExpressionOperator(Definition.PostfixUnaryExpression.OperatorType o)
		{
			switch (o)
			{
				case Definition.PostfixUnaryExpression.OperatorType.PostIncrement:
					return "++";
				case Definition.PostfixUnaryExpression.OperatorType.PostDecrement:
					return "--";
				default:
					throw new NotImplementedException("unknown operator " + Enum.GetName(o.GetType(), o));
			}
		}


		private string GetTypeSpecifier(Definition.TypeSpecifier t)
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

		private string GetExpression(Definition.Expression e)
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
				return string.Format("({0} {1} {2})", GetExpression(e2.Left), GetBinaryExpressionOperator(e2.Operator), GetExpression(e2.Right));
			}
			else if (e is Definition.AssignmentExpression)
			{
				var e2 = (Definition.AssignmentExpression)e;
				return string.Format("{0} = {1}", GetExpression(e2.Target), GetExpression(e2.Expression));

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
				return e2.Text;
			}
			else if (e is Definition.MemberAccessExpression)
			{
				var e2 = (Definition.MemberAccessExpression)e;

				var accessed = GetExpression(e2.Expression);
				if(accessed != string.Empty)
				{
					accessed += ".";
				}

				if (e2.EnumMember != null)
				{
					return string.Format("{0}{1}.{2}", (e2.Enum.Namespace ==null || e2.Enum.Namespace == "") ? "": e2.Enum.Namespace + "." , e2.Enum.Name, e2.EnumMember.Name);
				}
				else if (e2.Method != null)
				{
					return string.Format("{0}{1}", accessed, e2.Method.Name);
				}
				else if (e2.Property != null)
				{
					return string.Format("{0}{1}", accessed, e2.Property.Name);
				}
				else if (e2.Expression != null)
				{
					return string.Format("{0}{1}", accessed, e2.Name);
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
				return string.Format("new {0}({1})", GetTypeSpecifier(e2.Type), MakeExpressionList(e2.Args));

			}
			else if (e is Definition.ObjectArrayCreationExpression)
			{
				var e2 = (Definition.ObjectArrayCreationExpression)e;
				return string.Format("new {0}[{1}]", GetTypeSpecifier(e2.Type), MakeExpressionList(e2.Args));
			}
			else if (e is Definition.GenericMemberAccessExpression)
			{
				var e2 = (Definition.GenericMemberAccessExpression)e;
				return string.Format("{0}<{1}>", e2.Name, string.Join(",", Array.ConvertAll(e2.Types, GetTypeSpecifier)));
			}
			else if (e is Definition.TypeExpression)
			{
				var e2 = (Definition.TypeExpression)e;
				return string.Format("{0}", GetTypeSpecifier(e2.Type));
			}
			else
			{
				throw new NotImplementedException("unknown expression " + e.GetType().ToString());
			}
		}

		private string MakeExpressionList(Definition.Expression[] exps)
		{
			var isFirst = true;
			var ret = "";
			foreach (var a in exps)
			{
				if (isFirst)
				{
					isFirst = false;
				}
				else
				{
					ret += ", ";
				}
				ret += GetExpression(a);
			}
			return ret;
		}

		private void OutputStatement(Definition.Statement s)
		{
			if (s == null)
			{
				MakeIndent();
				Res.AppendLine("/* debug: null statement */");
			}
			else if (s is Definition.BlockStatement)
			{
				var s2 = (Definition.BlockStatement)s;
				foreach (var e in s2.Statements)
				{
					OutputStatement(e);
					// Res.AppendLine();

				}
			}
			else if (s is Definition.ContinueStatement)
			{
				MakeIndent();
				Res.AppendLine("continue;");
			}
			else if (s is Definition.ExpressionStatement)
			{
				MakeIndent();
				var s2 = (Definition.ExpressionStatement)s;
				Res.AppendFormat("{0};\r\n", GetExpression(s2.Expression));
			}
			else if (s is Definition.ForeachStatement)
			{
				MakeIndent();
				var s2 = (Definition.ForeachStatement)s;
				Res.AppendFormat("for({0} {1}: {2}) {{\r\n", GetTypeSpecifier(s2.Type), s2.Name, GetExpression(s2.Value));
				IndentDepth++;
				OutputStatement(s2.Statement);
				IndentDepth--;
				MakeIndent();
				Res.AppendLine("}");
			}
			else if (s is Definition.ForStatement)
			{
				MakeIndent();
				var s2 = (Definition.ForStatement)s;
				Res.AppendFormat("for({0} {1} = {2}; {3}; {4}) {{\r\n", GetTypeSpecifier(s2.Declaration.Type), s2.Declaration.Name, GetExpression(s2.Declaration.Value), GetExpression(s2.Condition), GetExpression(s2.Incrementor));
				IndentDepth++;
				OutputStatement(s2.Statement);
				IndentDepth--;
				MakeIndent();
				Res.AppendLine("}");
			}
			else if (s is Definition.IfStatement)
			{
				MakeIndent();
				var s2 = (Definition.IfStatement)s;
				Res.AppendFormat("if({0}) {{\r\n", GetExpression(s2.Condition));
				IndentDepth++;
				OutputStatement(s2.TrueStatement);
				IndentDepth--;
				MakeIndent();

				if (s2.FalseStatement != null)
				{
					Res.AppendLine("} else {");
					IndentDepth++;
					OutputStatement(s2.TrueStatement);
					IndentDepth--;
					MakeIndent();
					Res.AppendLine("}");
				}
				else
				{
					Res.AppendLine("}");
				}
			}
			else if (s is Definition.ReturnStatement)
			{
				MakeIndent();
				var s2 = (Definition.ReturnStatement)s;
				Res.AppendFormat("return {0};\r\n", GetExpression(s2.Return));
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
				MakeIndent();
				Res.AppendFormat("synchronized({0}) {{\r\n", GetExpression(s2.Expression));
				IndentDepth++;
				OutputStatement(s2.Statement);
				IndentDepth--;
				MakeIndent();
				Res.AppendLine("}");
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
			MakeBrief(f.Brief);
			MakeIndent();
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

		private void OutputFieldInInterface(Definition.FieldDef f)
		{
			MakeBrief(f.Brief);
			MakeIndent();
			Res.AppendFormat("{0} {1};", GetTypeSpecifier(f.Type), f.Name);
		}

		private void OutputConstructor(string name, Definition.ConstructorDef c)
		{
			MakeBrief(c.Brief);
			MakeIndent();

			Res.AppendFormat("{0} {1}({2}) {{\r\n", GetAccessLevel(c.AccessLevel), name, GetParamStr(c.Parameters));
			IndentDepth++;
			if (c.Initializer != null)
			{
				MakeIndent();
				Res.AppendFormat("{0}({1});\r\n", c.Initializer.ThisOrBase, string.Join(", ", c.Initializer.Arguments.ConvertAll(GetExpression)));
			}

			foreach (var s in c.Body)
			{
				OutputStatement(s);
			}
			IndentDepth--;
			MakeIndent();
			Res.AppendLine("}");
		}

		private void OutputMethod(Definition.MethodDef m)
		{
			Func<string> generics = () =>
			{
				if (m.TypeParameters.Count == 0) return string.Empty;
				return "<" + string.Join(",", m.TypeParameters.Select(_ => _.Name).ToArray()) + ">";
			};

			MakeBrief(m.Brief);
			MakeIndent();

			Res.AppendFormat("{3} {4} {5} {0} {1}({2}) {{\r\n", GetTypeSpecifier(m.ReturnType), m.Name, GetParamStr(m.Parameters), GetAccessLevel(m.AccessLevel), m.IsStatic ? "static " : "", generics());
			IndentDepth++;
			foreach (var s in m.Body)
			{
				OutputStatement(s);
			}
			IndentDepth--;
			MakeIndent();
			Res.AppendLine("}");
		}

		private void OutputMethodInInterface(Definition.MethodDef m)
		{
			MakeBrief(m.Brief);
			MakeIndent();

			Res.AppendFormat("{0} {1}({2});\r\n", GetTypeSpecifier(m.ReturnType), m.Name, GetParamStr(m.Parameters));
		}


		private void OutputProperty(Definition.PropertyDef p)
		{

			var needVariable = true;

			if (p.Setter != null)
			{
				if (p.Setter.Body != null)
				{
					MakeIndent();
					Res.AppendFormat("{2} {3}void set{0}({1} value) {{\r\n", p.Name, GetTypeSpecifier(p.Type), GetAccessLevel(p.AccessLevel), p.IsStatic ? "static " : "");
					IndentDepth++;
					OutputStatement(p.Setter.Body);
					IndentDepth--;
					MakeIndent();
					Res.AppendLine("}");
					needVariable = false;
				}
				else
				{
					MakeIndent();
					Res.AppendFormat("{2} {3}void set{0}({1} value) {{ {0} = value; }}\r\n", p.Name, GetTypeSpecifier(p.Type), GetAccessLevel(p.AccessLevel), p.IsStatic ? "static " : "");
				}
			}


			if (p.Getter != null)
			{
				if (p.Getter.Body != null)
				{
					MakeIndent();
					Res.AppendFormat("{2} {3}{0} get{1}() {{\r\n", GetTypeSpecifier(p.Type), p.Name, GetAccessLevel(p.AccessLevel), p.IsStatic ? "static " : "");
					IndentDepth++;
					OutputStatement(p.Getter.Body);
					IndentDepth--;
					MakeIndent();
					Res.AppendLine("}");
					needVariable = false;
				}
				else
				{
					MakeIndent();
					Res.AppendFormat("{2} {3}{0} get{1}() {{ return {1}; }}\r\n", GetTypeSpecifier(p.Type), p.Name, GetAccessLevel(p.AccessLevel), p.IsStatic ? "static " : "");
				}

			}

			if (needVariable)
			{
				MakeBrief(p.Brief);
				MakeIndent();
				Res.AppendFormat("private {0} {1};\r\n", GetTypeSpecifier(p.Type), p.Name);
			}

		}

		private void OutputPropertyInInterface(Definition.PropertyDef p)
		{
			if (p.Setter != null)
			{
				MakeIndent();
				Res.AppendFormat("void set{0}({1} value);\r\n", p.Name, GetTypeSpecifier(p.Type));
			}


			if (p.Getter != null)
			{
				MakeIndent();
				Res.AppendFormat("{0} get{1}();\r\n", GetTypeSpecifier(p.Type), p.Name);
			}
		}

		private void OutputClass(Definition.ClassDef cs)
		{
			MakeBrief(cs.Brief);
			MakeIndent();

			List<Definition.TypeSpecifier> bases = new List<Definition.TypeSpecifier>();
			List<Definition.TypeSpecifier> interfaces = new List<Definition.TypeSpecifier>();

			foreach(var b in cs.BaseTypes)
			{
				var simple = b as Definition.SimpleType;
				var gene = b as Definition.GenericType;
			
				if(simple != null)
				{
					if(simple.TypeKind == Definition.SimpleTypeKind.Interface)
					{
						interfaces.Add(b);
					}
					else
					{
						bases.Add(b);
					}
				}

				if(gene != null)
				{
					bases.Add(b);
				}
			}

			Func<string> extends = () =>
				{
					if (bases.Count == 0) return string.Empty;
					return " extends " + string.Join(",", GetTypeSpecifier(bases[0]));
				};

			Func<string> implements = () =>
			{
				if (interfaces.Count == 0) return string.Empty;
				return " implements " + string.Join(",", interfaces.Select(_ => GetTypeSpecifier(_)));
			};

			Func<string> generics = () =>
			{
				if (cs.TypeParameters.Count == 0) return string.Empty;
				return "<" + string.Join(",", cs.TypeParameters.Select(_=>_.Name).ToArray()) + ">";
			};

			Res.AppendFormat("{0} {1} class {2}{3} {4} {{\r\n",
				GetAccessLevel(cs.AccessLevel),
				cs.IsAbstract ? "abstract " : "",
				cs.Name,
				generics(),
				extends(),
				implements());
			IndentDepth++;
			

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
					MakeIndent();
					Res.AppendLine("@Override");
					MakeIndent();
					Res.AppendLine("protected void finalize() {");
					IndentDepth++;
					if (cs.BaseTypes != null && cs.BaseTypes.Count > 0)
					{
						MakeIndent();
						Res.AppendLine("try { super.finalize(); } finally {");
						IndentDepth++;
					}
					foreach (var s in d.Body)
					{
						OutputStatement(s);
					}
					if (cs.BaseTypes != null)
					{
						IndentDepth--;
						MakeIndent();
						Res.AppendLine("}");

					}
					IndentDepth--;
					MakeIndent();
					Res.AppendLine("}");
				}
			}

			IndentDepth--;
			MakeIndent();
			Res.AppendFormat("}}\r\n");
		}

		private void OutputEnum(Definition.EnumDef es)
		{
			MakeBrief(es.Brief);
			MakeIndent();
			Res.AppendFormat("public enum {0} {{\r\n", es.Name);
			IndentDepth++;

			int count = 0;
			foreach (var e in es.Members)
			{
				MakeBrief(e.Brief);
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
			MakeIndent();
			Res.AppendFormat("}}\r\n");
		}


		private void OutputStruct(Definition.StructDef ss)
		{
			MakeBrief(ss.Brief);
			MakeIndent();
			Res.AppendFormat("{1} class {0} {{\r\n", ss.Name, GetAccessLevel(ss.AccessLevel));
			IndentDepth++;

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
				MakeIndent();
				var name = ss.Name;
				var constructor = "public " + name + "() {}";
				Res.AppendLine(constructor);
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
					MakeIndent();
					Res.AppendLine("@Override");
					MakeIndent();
					Res.AppendLine("protected void finalize() {");
					IndentDepth++;
					if (ss.BaseTypes != null && ss.BaseTypes.Count > 0)
					{
						MakeIndent();
						Res.AppendLine("try { super.finalize(); } finally {");
						IndentDepth++;
					}
					foreach (var s in d.Body)
					{
						OutputStatement(s);
					}
					if (ss.BaseTypes != null)
					{
						IndentDepth--;
						MakeIndent();
						Res.AppendLine("}");

					}
					IndentDepth--;
					MakeIndent();
					Res.AppendLine("}");
				}
			}


			IndentDepth--;
			MakeIndent();
			Res.AppendFormat("}}");
		}

		private void OutputInterface(Definition.InterfaceDef i) {
			MakeBrief(i.Brief);
			MakeIndent();
			Res.AppendFormat("{1} interface {0} {{\r\n", i.Name, GetAccessLevel(i.AccessLevel));
			IndentDepth++;

			if (i.Fields != null)
			{
				foreach (var f in i.Fields)
				{
					OutputFieldInInterface(f);
				}
			}

			foreach (var p in i.Properties)
			{
				OutputPropertyInInterface(p);
			}

			foreach (var m in i.Methods)
			{
				OutputMethodInInterface(m);
			}

			IndentDepth--;
			MakeIndent();
			Res.AppendFormat("}}");
		}

		public void Translate(string targetDir, Definition.Definitions definisions)
		{
			var sep = Path.DirectorySeparatorChar.ToString();
			foreach (Definition.EnumDef e in definisions.Enums)
			{
				IndentDepth = 0;
				if (e.IsDefinedBySWIG) { continue; }
				var subDir = targetDir + string.Join(sep, e.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + sep + e.Name + ".java");
				Res.AppendFormat("package {0};\r\n\r\n", e.Namespace);
				OutputEnum(e);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

			foreach (var c in definisions.Classes)
			{
				IndentDepth = 0;
				if (c.IsDefinedBySWIG) { continue; }
				if (c.IsDefinedDefault) { continue; }
				if (!c.IsExported) { continue; }

				var subDir = targetDir + string.Join(sep, c.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + sep + c.Name + ".java");
				Res.AppendFormat("package {0};\r\n\r\n", c.Namespace);
				OutputClass(c);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

			foreach (var s in definisions.Structs)
			{
				if (s.IsDefinedDefault) { continue; }

				IndentDepth = 0;
				var subDir = targetDir + string.Join(sep, s.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + sep + s.Name + ".java");
				Res.AppendFormat("package {0};\r\n\r\n", s.Namespace);
				OutputStruct(s);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

			foreach (var i in definisions.Interfaces)
			{
				IndentDepth = 0;
				var subDir = targetDir + string.Join(sep, i.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + sep + i.Name + ".java");
				Res.AppendFormat("package {0};\r\n\r\n", i.Namespace);
				OutputInterface(i);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

		}
	}
}

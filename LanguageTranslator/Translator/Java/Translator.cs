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
					return "internal";
				case LanguageTranslator.Definition.AccessLevel.ProtectedInternal:
					return "protected internal";
				default:
					throw new NotImplementedException("unknown access modifier " + Enum.GetName(a.GetType(), a));
			}
		}

		private string GetBinaryExpressionOperator(Definition.BinaryExpression.OperatorType o) {
			switch (o)
			{
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Add:
					return "+";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Subtract:
					return "-";
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

		private string GetTypeSpecifier(Definition.TypeSpecifier t) {
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
				var e2 = (Definition.BinaryExpression) e;

				// as 対応
				if (e2.Operator == Definition.BinaryExpression.OperatorType.As)
				{
					return string.Format("({0} instanceof {1}? ({1}){0}: null)", GetExpression(e2.Left), GetExpression(e2.Right));

				}
				return string.Format("({0} {1} {2})", GetExpression(e2.Left), GetBinaryExpressionOperator(e2.Operator), GetExpression(e2.Right));
			}
			else if (e is Definition.AssignmentExpression)
			{
				var e2 = (Definition.AssignmentExpression) e;
				return string.Format("({0} = {1})", GetExpression(e2.Target), GetExpression(e2.Expression));

			}
			else if (e is Definition.CastExpression)
			{
				var e2 = (Definition.CastExpression)e;
				return string.Format("(({0}){1})", GetTypeSpecifier(e2.Type), GetExpression(e2.Expression));

			}
			else if (e is Definition.ElementAccessExpression)
			{
				var e2 = (Definition.ElementAccessExpression)e;
				return string.Format("({0}[{1}])", GetExpression(e2.Value), GetExpression(e2.Arg));
			}
			else if (e is Definition.IdentifierNameExpression)
			{
				var e2 = (Definition.IdentifierNameExpression)e;
				return e2.Name;
			}
			else if (e is Definition.InvocationExpression)
			{
				var e2 = (Definition.InvocationExpression)e;
				return string.Format("({0}({1}))", GetExpression(e2.Method), string.Join(", ", Array.ConvertAll(e2.Args, GetExpression)));
			}
			else if (e is Definition.LiteralExpression)
			{
				var e2 = (Definition.LiteralExpression)e;
				return e2.Text;
			}
			else if (e is Definition.MemberAccessExpression)
			{
				var e2 = (Definition.MemberAccessExpression)e;
				if (e2.EnumMember != null)
				{
					return string.Format("{0}.{1}", e2.Enum.Name, e2.EnumMember.Name);
				}
				else if (e2.Method != null)
				{
					return string.Format("{0}.{1}", GetExpression(e2.Expression), e2.Method.Name);
				}
				else if (e2.Expression != null)
				{
					return string.Format("{0}.{1}", GetExpression(e2.Expression), e2.Name);
				}
				else
				{
					return e2.Name;
				}
				
			}
			else if (e is Definition.ObjectCreationExpression)
			{
				var e2 = (Definition.ObjectCreationExpression)e;
				return string.Format("new {0}({1})", e2.Type, string.Join(", ", Array.ConvertAll(e2.Args, GetExpression)));
			}
			else if (e is Definition.PrefixUnaryExpression)
			{
				var e2 = (Definition.PrefixUnaryExpression)e;
				return string.Format("({0}{1})", GetPrefixUnaryExpressionOperator(e2.Type), GetExpression(e2.Expression));
			}
			else if (e is Definition.PostfixUnaryExpression)
			{
				var e2 = (Definition.PostfixUnaryExpression)e;
				return string.Format("({0}{1})", GetExpression(e2.Operand), GetPostfixUnaryExpressionOperator(e2.Type));
			}
			else if (e is Definition.ThisExpression)
			{
				return "this";
			}
			else if (e is Definition.BaseExpression)
			{
				return "super";
			}
			else
			{
				throw new NotImplementedException("unknown expression " + e.GetType().ToString());
			}
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
			} else if (s is Definition.ContinueStatement) {
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
				Res.AppendFormat("for(;{0}; {1}) {{\r\n", GetExpression(s2.Condition), GetExpression(s2.Incrementor));
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
				Res.AppendFormat("synchronized({0}) {{", GetExpression(s2.Expression));
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
				res.Append(GetTypeSpecifier(p.Type));
				res.Append(" ");
				res.Append(p.Name);
			}
			return res.ToString();
		}

		private void OutputEnum(Definition.EnumDef es)
		{
			MakeBrief(es.Brief);
			MakeIndent();
			Res.AppendFormat("public enum {0} {{\r\n", es.Name);
			IndentDepth++;
			foreach (var e in es.Members)
			{
				MakeBrief(e.Brief);
				MakeIndent();
				Res.Append(e.Name);
				if (e.Value != null)
				{
					Res.AppendFormat(" = {0},\r\n", GetExpression(e.Value));
				}
				else
				{
					Res.AppendLine(",");
				}
				
			}
			IndentDepth--;
			MakeIndent();
			Res.AppendFormat("}}\r\n");
		}


		private void OutputClass(Definition.ClassDef cs)
		{
			MakeBrief(cs.Brief);
			MakeIndent();
			Res.AppendFormat("{1} {2}class {0} {{\r\n", cs.Name, GetAccessLevel(cs.AccessLevel), cs.IsAbstract? "abstract ":"");
			IndentDepth++;

			
			foreach (var f in cs.Fields)
			{
				MakeBrief(f.Brief);
				MakeIndent();
				Res.AppendFormat("{2} {3}{0} {1}", GetTypeSpecifier(f.Type), f.Name, GetAccessLevel(f.AccessLevel), f.IsStatic? "static ":"");
				if (f.Initializer != null)
				{
					Res.AppendFormat(" = {0};\r\n", GetExpression(f.Initializer));
				}
				else
				{
					Res.AppendLine(";");
				}
			}

			foreach (var p in cs.Properties)
			{
				var needVariable = true;
				
				if (p.Setter != null)
				{
					if (p.Setter.Body != null)
					{
						MakeIndent();
						Res.AppendFormat("{2} {3}void set{0}({1} value) {{\r\n", p.Name, GetTypeSpecifier(p.Type), GetAccessLevel(p.AccessLevel), p.IsStatic? "static ": "");
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
						Res.AppendFormat("{2} {3}{0} get{1}() {{\r\n", GetTypeSpecifier(p.Type), p.Name, GetAccessLevel(p.AccessLevel), p.IsStatic? "static ": "");
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

			foreach (var m in cs.Methods)
			{
				MakeBrief(m.Brief);
				MakeIndent();

				Res.AppendFormat("{3} {4}{0} {1}({2}) {{\r\n", GetTypeSpecifier(m.ReturnType), m.Name, GetParamStr(m.Parameters), GetAccessLevel(m.AccessLevel), m.IsStatic ? "static " : "");
				IndentDepth++;
				foreach (var s in m.Body)
				{
					OutputStatement(s);
				}
				IndentDepth--;
				MakeIndent();
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
				if (e.IsDefinedBySWIG) { continue; }
				var subDir = targetDir + string.Join("\\", e.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + e.Name + ".java");
				OutputEnum(e);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

			foreach (var c in definisions.Classes)
			{
				if (c.IsDefinedBySWIG) { continue; }
				var subDir = targetDir + string.Join("\\", c.Namespace.Split('.'));
				System.IO.Directory.CreateDirectory(subDir);
				var of = System.IO.File.CreateText(subDir + "\\" + c.Name + ".java");
				OutputClass(c);
				of.Write(Res.ToString());
				of.Close();
				Res.Clear();
			}

		}
	}
}

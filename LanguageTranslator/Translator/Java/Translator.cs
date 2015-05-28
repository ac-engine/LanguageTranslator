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

		private string GetBinaryExpressionOperator(Definition.BinaryExpression.OperatorType o) {
			switch (o)
			{
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Add:
					return "+";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.Subtract:
					return "-";
				case LanguageTranslator.Definition.BinaryExpression.OperatorType.EqualsEquals:
					return "==";
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
				case Definition.PrefixUnaryExpression.OperatorType.PlusPlus:
					return "++";
				case Definition.PrefixUnaryExpression.OperatorType.MinusMinus:
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
			else if (e is Definition.ThisExpression)
			{
				return "this";
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
				Res.AppendFormat("{0};\n", GetExpression(s2.Expression));
			}
			else if (s is Definition.ForeachStatement)
			{
				MakeIndent();
				var s2 = (Definition.ForeachStatement)s;
				Res.AppendFormat("for({0} {1}: {2}) {{\n", GetTypeSpecifier(s2.Type), s2.Name, GetExpression(s2.Value));
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
				Res.AppendFormat("for(;{0}; {1}) {{\n", GetExpression(s2.Condition), GetExpression(s2.Incrementor));
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
				Res.AppendFormat("if({0}) {{\n", GetExpression(s2.Condition));
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
				Res.AppendFormat("return {0};\n", GetExpression(s2.Return));
			}
			else if (s is Definition.VariableDeclarationStatement)
			{
				MakeIndent();
				var s2 = (Definition.VariableDeclarationStatement)s;
				Res.AppendFormat("{0} {1}", GetTypeSpecifier(s2.Type), s2.Name);
				if (s2.Value != null)
				{
					Res.AppendFormat(" = {0};\n", GetExpression(s2.Value));
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
			Res.AppendFormat("public enum {0} {{\n", es.Name);
			IndentDepth++;
			foreach (var e in es.Members)
			{
				MakeBrief(e.Brief);
				MakeIndent();
				Res.Append(e.Name);
				if (e.Value != null)
				{
					Res.AppendFormat(" = {0},\n", GetExpression(e.Value));
				}
				else
				{
					Res.AppendLine(",");
				}
				
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
				Res.AppendFormat("public {0} {1}", GetTypeSpecifier(f.Type), f.Name);
				if (f.Initializer != null)
				{
					Res.AppendFormat(" = {0};\n", GetExpression(f.Initializer));
				}
				else
				{
					Res.AppendLine(";");
				}
			}

			foreach (var p in cs.Properties)
			{
				var needVariable = true;
				
				if (p.Setter != null && p.Setter.Body != null)
				{
					MakeIndent();
					Res.AppendFormat("public void set{0}({1} value) {{\n", p.Name, GetTypeSpecifier(p.Type));
					IndentDepth++;
					OutputStatement(p.Setter.Body);
					IndentDepth--;
					MakeIndent();
					Res.AppendLine("}");
					needVariable = false;
				}
				
				
				if (p.Getter != null && p.Getter.Body != null)
				{
					MakeIndent();
					Res.AppendFormat("public void get{0}() {{\n", p.Name);
					IndentDepth++;
					OutputStatement(p.Getter.Body);
					IndentDepth--;
					MakeIndent();
					Res.AppendLine("}");
					needVariable = false;
				}

				if (needVariable)
				{
					MakeBrief(p.Brief);
					MakeIndent();
					Res.AppendFormat("public {0} {1};\n", GetTypeSpecifier(p.Type), p.Name);
				}
				
			}

			foreach (var m in cs.Methods)
			{
				MakeBrief(m.Brief);
				MakeIndent();

				Res.AppendFormat("public {0} {1}({2}) {{\n", GetTypeSpecifier(m.ReturnType), m.Name, GetParamStr(m.Parameters));
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

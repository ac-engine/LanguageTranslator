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
				t2.Namespace = t2.Namespace == null? "": t2.Namespace + ".";
				return string.Format("{0}{1}", t2.Namespace, t2.TypeName);
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
			// not implemented
			if (e is Definition.BinaryExpression)
			{
				var e2 = (Definition.BinaryExpression) e;
				return string.Format("({0} {1} {2})", GetExpression(e2.Left), GetBinaryExpressionOperator(e2.Operator), GetExpression(e2.Right));
			}
			else if (e is Definition.AssignmentExpression)
			{
				var e2 = (Definition.AssignmentExpression) e;
				return string.Format("({0} = {2})", GetExpression(e2.Target), GetExpression(e2.Expression));

			}
			else if (e is Definition.CastExpression)
			{
				var e2 = (Definition.CastExpression)e;
				return string.Format("(({0}){2})", GetTypeSpecifier(e2.Type), GetExpression(e2.Expression));

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
				return string.Format("\"{0}\"", e2.Text);
			}
			else if (e is Definition.MemberAccessExpression)
			{
				var e2 = (Definition.MemberAccessExpression)e;
				return string.Format("{0}.{1}", GetExpression(e2.Expression), (e2.EnumMember));
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

			if (s is Definition.BlockStatement)
			{
				var s2 = (Definition.BlockStatement)s;
				foreach (var e in s2.Statements)
				{
					MakeIndent();
					OutputStatement(e);
					Res.AppendLine();
					
				}
			} else if (s is Definition.ContinueStatement) {
				Res.AppendLine("continue;");
			}
			else if (s is Definition.ExpressionStatement)
			{
				var s2 = (Definition.ExpressionStatement)s;
				Res.AppendFormat("{0};", GetExpression(s2.Expression));
			}
			else if (s is Definition.ForeachStatement)
			{
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
				var s2 = (Definition.ReturnStatement)s;
				Res.AppendFormat("return {0};", GetExpression(s2.Return));
			}
			else if (s is Definition.VariableDeclarationStatement)
			{
				var s2 = (Definition.VariableDeclarationStatement)s;
				Res.AppendFormat("{0} {1}", GetTypeSpecifier(s2.Type), s2.Name);
				if (s2.Value != null)
				{
					Res.AppendFormat(" = {0};\n");
				}
				else
				{
					Res.AppendLine(";");
				}
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
				res.Append(p.Type);
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
				Res.AppendFormat("{0} = {1},\n", e.Name, GetExpression(e.Value));
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
				Res.AppendFormat("public {0} {1} = {2};\n", f.Type, f.Name, GetExpression(f.Initializer));
			}

			foreach (var p in cs.Properties)
			{
				MakeBrief(p.Brief);
				MakeIndent();
				Res.AppendFormat("private {0} {1};\n", p.Type, p.Name);
				MakeIndent();

				Res.AppendFormat("public void set{0} {{\n", p.Name);
				IndentDepth++;
				if (p.Setter.Body != null)
				{
					OutputStatement(p.Setter.Body);
				}
				IndentDepth--;
				MakeIndent();
				Res.AppendLine("}");

				Res.AppendFormat("public void get{0} {{\n", p.Name);
				IndentDepth++;
				if (p.Getter.Body != null)
				{
					OutputStatement(p.Getter.Body);
				}
				IndentDepth--;
				Res.AppendLine("}");
			}

			foreach (var m in cs.Methods)
			{
				MakeBrief(m.Brief);
				MakeIndent();

				Res.AppendFormat("public {0} {1}({2}) {{\n", m.ReturnType, m.Name, GetParamStr(m.Parameters));
				IndentDepth++;
				foreach (var s in m.Body)
				{
					OutputStatement(s);
				}
				IndentDepth--;
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
				OutputEnum(e);
			}

			foreach (var c in definisions.Classes)
			{
				OutputClass(c);
			}
			Console.Write(Res.ToString());
			Console.ReadLine();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Translator
{
	enum Language
	{
		Java,
		Cpp,
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
			ConvertMethods();
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

		#region Edit
		void Edit()
		{
			foreach (var func in editFunc)
			{
				Edit(func, definitions.Classes);
				Edit(func, definitions.Structs);
				Edit(func, definitions.Interfaces);
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

				Edit(func, arr[i].Constructors);
				Edit(func, arr[i].Destructors);
				Edit(func, arr[i].Methods);
				Edit(func, arr[i].Fields);
				Edit(func, arr[i].Properties);
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, List<Definition.StructDef> arr)
		{
			for (int i = 0; i < arr.Count; i++)
			{
				var r = func(arr[i]);
				if (r.Item2 != null)
				{
					arr[i] = (Definition.StructDef)r.Item2;
					if (!r.Item1) continue;
				}

				Edit(func, arr[i].Constructors);
				Edit(func, arr[i].Destructors);
				Edit(func, arr[i].Methods);
				Edit(func, arr[i].Fields);
				Edit(func, arr[i].Properties);
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, List<Definition.InterfaceDef> arr)
		{
			for (int i = 0; i < arr.Count; i++)
			{
				var r = func(arr[i]);
				if (r.Item2 != null)
				{
					arr[i] = (Definition.InterfaceDef)r.Item2;
					if (!r.Item1) continue;
				}

				Edit(func, arr[i].Methods);
				Edit(func, arr[i].Properties);
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, List<Definition.ConstructorDef> arr)
		{
			for (int i = 0; i < arr.Count; i++)
			{
				var r = func(arr[i]);
				if (r.Item2 != null)
				{
					arr[i] = (Definition.ConstructorDef)r.Item2;
					if (!r.Item1) continue;
				}

				Edit(func, arr[i].Body);
			}
		}

		void Edit(Func<object, Tuple<bool, object>> func, List<Definition.DestructorDef> arr)
		{
			for (int i = 0; i < arr.Count; i++)
			{
				var r = func(arr[i]);
				if (r.Item2 != null)
				{
					arr[i] = (Definition.DestructorDef)r.Item2;
					if (!r.Item1) continue;
				}

				Edit(func, arr[i].Body);
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

				for (int j = 0; j < arr[i].Parameters.Count; j++)
				{
					Edit(func, ref arr[i].Parameters[j].Type);
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

				if (arr[i].Type != null)
				{
					Edit(func, ref arr[i].Type);
				}

				if (arr[i].Initializer != null)
				{
					Edit(func, ref arr[i].Initializer);
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

				if (arr[i].Type != null)
				{
					Edit(func, ref arr[i].Type);
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
			else if (s is Definition.WhileStatement)
			{
				var s_ = s as Definition.WhileStatement;

				Edit(func, ref s_.Condition);
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
			else if (s is Definition.BreakStatement)
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
				else if (s is Definition.WhileStatement)
				{
					var s_ = s as Definition.WhileStatement;

					Edit(func, ref s_.Condition);
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
				else if (s is Definition.BreakStatement)
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

			Edit(func, ref e.SelfType);

			if (e is Definition.MemberAccessExpression)
			{
				var e_ = e as Definition.MemberAccessExpression;
				Edit(func, ref e_.Expression);
			}
			else if (e is Definition.GenericNameExpression)
			{
				var e_ = e as Definition.GenericNameExpression;
				for (int i = 0; i < e_.Types.Length; i++)
				{
					Edit(func, ref e_.Types[i]);
				}
			}
			else if (e is Definition.CastExpression)
			{
				var e_ = e as Definition.CastExpression;
				Edit(func, ref e_.Type);
				Edit(func, ref e_.Expression);
			}
			else if (e is Definition.LiteralExpression)
			{
			}
			else if (e is Definition.InvocationExpression)
			{
				var e_ = e as Definition.InvocationExpression;
				Edit(func, ref e_.Method);
				for (int i = 0; i < e_.Args.Length; i++)
				{
					Edit(func, ref e_.Args[i]);
				}
			}
			else if (e is Definition.ObjectCreationExpression)
			{
				var e_ = e as Definition.ObjectCreationExpression;
				Edit(func, ref e_.Type);
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
				Edit(func, ref e_.Type);
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
			}
			else if (e is Definition.InitializerExpression)
			{
				var e_ = e as Definition.InitializerExpression;
				for (int i = 0; i < e_.Expressions.Length; i++)
				{
					Edit(func, ref e_.Expressions[i]);
				}
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

			if (typeSpecifier == null)
			{
				return;
			}
			else if (typeSpecifier is Definition.SimpleType)
			{
				return;
			}
			else if (typeSpecifier is Definition.ArrayType)
			{
				var src = typeSpecifier as Definition.ArrayType;
				Definition.TypeSpecifier t = src.BaseType;
				Edit(func, ref t);
				src.BaseType = (Definition.SimpleType)t;
			}
			else if (typeSpecifier is Definition.GenericType)
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

		#endregion

		#region Method
		void ConvertMethods()
		{
			foreach (var def in definitions.Structs)
			{
				foreach (var m in def.Methods)
				{
					var methodString = GetTypeString(def.Namespace, def.Name) + "." + m.Name;
					if (methodConverter.ContainsKey(methodString))
					{
						m.Name = methodConverter[methodString];
					}
				}

				foreach (var p in def.Properties)
				{
					var methodString = GetTypeString(def.Namespace, def.Name) + "." + p.Name;
					if (methodConverter.ContainsKey(methodString))
					{
						p.Name = methodConverter[methodString];
					}
				}
			}

			foreach (var def in definitions.Classes)
			{
				foreach (var m in def.Methods)
				{
					var methodString = GetTypeString(def.Namespace, def.Name) + "." + m.Name;
					if (methodConverter.ContainsKey(methodString))
					{
						m.Name = methodConverter[methodString];
					}
				}

				foreach (var p in def.Properties)
				{
					var methodString = GetTypeString(def.Namespace, def.Name) + "." + p.Name;
					if (methodConverter.ContainsKey(methodString))
					{
						p.Name = methodConverter[methodString];
					}
				}
			}

			foreach (var def in definitions.Interfaces)
			{
				foreach (var m in def.Methods)
				{
					var methodString = GetTypeString(def.Namespace, def.Name) + "." + m.Name;
					if (methodConverter.ContainsKey(methodString))
					{
						m.Name = methodConverter[methodString];
					}
				}

				foreach (var p in def.Properties)
				{
					var methodString = GetTypeString(def.Namespace, def.Name) + "." + p.Name;
					if (methodConverter.ContainsKey(methodString))
					{
						p.Name = methodConverter[methodString];
					}
				}
			}
		}
		#endregion

		#region TypeName
		void ConvertTypeName()
		{
			foreach (var def in definitions.Structs)
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
					field.Type = ConvertTypeName(field.Type);
					field.Initializer = ConvertTypeName(field.Initializer);
				}

				foreach (var prop in def.Properties)
				{
					prop.Type = ConvertTypeName(prop.Type);
					if (prop.Getter != null)
					{
						prop.Getter.Body = ConvertTypeName(prop.Getter.Body);
					}

					if (prop.Setter != null)
					{
						prop.Setter.Body = ConvertTypeName(prop.Setter.Body);
					}
				}

				foreach (var m in def.Operators)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertTypeName(paramDef.Type);
					}

					m.ReturnType = ConvertTypeName(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertTypeName(_)).ToList();
				}

				foreach (var m in def.Constructors)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertTypeName(paramDef.Type);
					}

					m.Body = m.Body.Select(_ => ConvertTypeName(_)).ToList();
				}

				foreach (var m in def.Destructors)
				{
					m.Body = m.Body.Select(_ => ConvertTypeName(_)).ToList();
				}

				foreach (var m in def.Methods)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertTypeName(paramDef.Type);
					}

					foreach (var paramDef in m.TypeParameters)
					{
						paramDef.BaseTypeConstraints = paramDef.BaseTypeConstraints.Select(_ => ConvertTypeName(_)).ToList();
					}

					m.ReturnType = ConvertTypeName(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertTypeName(_)).ToList();
				}
			}

			foreach (var def in definitions.Classes)
			{
				{
					var types = def.BaseTypes.Select(_ => ConvertTypeName(_)).ToArray();
					def.BaseTypes.Clear();
					def.BaseTypes.AddRange(types);
				}

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
					field.Type = ConvertTypeName(field.Type);
					field.Initializer = ConvertTypeName(field.Initializer);
				}

				foreach (var prop in def.Properties)
				{
					prop.Type = ConvertTypeName(prop.Type);
					if (prop.Getter != null)
					{
						prop.Getter.Body = ConvertTypeName(prop.Getter.Body);
					}

					if (prop.Setter != null)
					{
						prop.Setter.Body = ConvertTypeName(prop.Setter.Body);
					}
				}

				foreach (var m in def.Operators)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertTypeName(paramDef.Type);
					}

					m.ReturnType = ConvertTypeName(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertTypeName(_)).ToList();
				}

				foreach (var m in def.Constructors)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertTypeName(paramDef.Type);
					}

					m.Body = m.Body.Select(_ => ConvertTypeName(_)).ToList();
				}

				foreach (var m in def.Destructors)
				{
					m.Body = m.Body.Select(_ => ConvertTypeName(_)).ToList();
				}

				foreach (var m in def.Methods)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertTypeName(paramDef.Type);
					}

					foreach (var paramDef in m.TypeParameters)
					{
						paramDef.BaseTypeConstraints = paramDef.BaseTypeConstraints.Select(_ => ConvertTypeName(_)).ToList();
					}

					m.ReturnType = ConvertTypeName(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertTypeName(_)).ToList();
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
					prop.Type = ConvertTypeName(prop.Type);
					if (prop.Getter != null)
					{
						prop.Getter.Body = ConvertTypeName(prop.Getter.Body);
					}

					if (prop.Setter != null)
					{
						prop.Setter.Body = ConvertTypeName(prop.Setter.Body);
					}
				}

				foreach (var m in def.Methods)
				{
					foreach (var paramDef in m.Parameters)
					{
						paramDef.Type = ConvertTypeName(paramDef.Type);
					}

					foreach (var paramDef in m.TypeParameters)
					{
						paramDef.BaseTypeConstraints = paramDef.BaseTypeConstraints.Select(_ => ConvertTypeName(_)).ToList();
					}

					m.ReturnType = ConvertTypeName(m.ReturnType);
					m.Body = m.Body.Select(_ => ConvertTypeName(_)).ToList();
				}
			}

			foreach (var def in definitions.Enums)
			{
				{
					var typeString = GetTypeString(def.Namespace, def.Name);
					if (typeConverter.ContainsKey(typeString))
					{
						def.Namespace = typeConverter[typeString].Item1;
						def.Name = typeConverter[typeString].Item2;
					}
				}

				foreach (var m in def.Members)
				{
					m.Value = ConvertTypeName(m.Value);
				}
			}
		}

		Definition.Statement ConvertTypeName(Definition.Statement s)
		{
			if (s == null) return s;

			if (s is Definition.BlockStatement)
			{
				var s_ = s as Definition.BlockStatement;
				s_.Statements = s_.Statements.Select(_ => ConvertTypeName(_)).ToArray();
				return s_;
			}
			else if (s is Definition.VariableDeclarationStatement)
			{
				var s_ = s as Definition.VariableDeclarationStatement;
				s_.Type = ConvertTypeName(s_.Type);
				s_.Value = ConvertTypeName(s_.Value);
				return s_;
			}
			else if (s is Definition.ForeachStatement)
			{
				var s_ = s as Definition.ForeachStatement;
				s_.Type = ConvertTypeName(s_.Type);
				s_.Value = ConvertTypeName(s_.Value);
				s_.Statement = ConvertTypeName(s_.Statement);
				return s_;
			}
			else if (s is Definition.ForStatement)
			{
				var s_ = s as Definition.ForStatement;
				s_.Declaration = ConvertTypeName(s_.Declaration) as Definition.VariableDeclarationStatement;
				s_.Condition = ConvertTypeName(s_.Condition);
				s_.Incrementor = ConvertTypeName(s_.Incrementor);
				s_.Statement = ConvertTypeName(s_.Statement);
				return s_;
			}
			else if (s is Definition.WhileStatement)
			{
				var s_ = s as Definition.WhileStatement;
				s_.Condition = ConvertTypeName(s_.Condition);
				s_.Statement = ConvertTypeName(s_.Statement);
				return s_;
			}
			else if (s is Definition.IfStatement)
			{
				var s_ = s as Definition.IfStatement;
				s_.Condition = ConvertTypeName(s_.Condition);
				s_.TrueStatement = ConvertTypeName(s_.TrueStatement);
				s_.FalseStatement = ConvertTypeName(s_.FalseStatement);
				return s_;
			}
			else if (s is Definition.ReturnStatement)
			{
				var s_ = s as Definition.ReturnStatement;
				s_.Return = ConvertTypeName(s_.Return);
				return s_;
			}
			else if (s is Definition.ContinueStatement)
			{
				return s;
			}
			else if (s is Definition.BreakStatement)
			{
				return s;
			}
			else if (s is Definition.ExpressionStatement)
			{
				var s_ = s as Definition.ExpressionStatement;
				s_.Expression = ConvertTypeName(s_.Expression);
				return s_;
			}
			else if (s is Definition.LockStatement)
			{
				var s_ = s as Definition.LockStatement;
				s_.Expression = ConvertTypeName(s_.Expression);
				s_.Statement = ConvertTypeName(s_.Statement);
				return s_;
			}

			throw new Exception();
		}

		Definition.Expression ConvertTypeName(Definition.Expression e)
		{
			if (e == null)
			{
				return e;
			}

			e.SelfType = ConvertTypeName(e.SelfType);

			if (e is Definition.MemberAccessExpression)
			{
				var e_ = e as Definition.MemberAccessExpression;
				e_.Expression = ConvertTypeName(e_.Expression);
				return e_;
			}
			else if (e is Definition.GenericNameExpression)
			{
				var e_ = e as Definition.GenericNameExpression;
				e_.Types = e_.Types.Select(_ => ConvertTypeName(_)).ToArray();
				return e_;
			}
			else if (e is Definition.CastExpression)
			{
				var e_ = e as Definition.CastExpression;
				e_.Type = ConvertTypeName(e_.Type);
				e_.Expression = ConvertTypeName(e_.Expression);
				return e_;
			}
			else if (e is Definition.LiteralExpression)
			{
				return e;
			}
			else if (e is Definition.InvocationExpression)
			{
				var e_ = e as Definition.InvocationExpression;
				e_.Method = ConvertTypeName(e_.Method);
				e_.Args = e_.Args.Select(_ => ConvertTypeName(_)).ToArray();
				return e_;
			}
			else if (e is Definition.ObjectCreationExpression)
			{
				var e_ = e as Definition.ObjectCreationExpression;
				e_.Type = ConvertTypeName(e_.Type);
				e_.Args = e_.Args.Select(_ => ConvertTypeName(_)).ToArray();
				return e_;
			}
			else if (e is Definition.AssignmentExpression)
			{
				var e_ = e as Definition.AssignmentExpression;
				e_.Target = ConvertTypeName(e_.Target);
				e_.Expression = ConvertTypeName(e_.Expression);
				return e_;
			}
			else if (e is Definition.ElementAccessExpression)
			{
				var e_ = e as Definition.ElementAccessExpression;
				e_.Value = ConvertTypeName(e_.Value);
				e_.Arg = ConvertTypeName(e_.Arg);
				return e_;
			}
			else if (e is Definition.ThisExpression)
			{
				return e;
			}
			else if (e is Definition.IdentifierNameExpression)
			{
				var e_ = e as Definition.IdentifierNameExpression;
				e_.Type = ConvertTypeName(e_.Type);
				return e_;
			}
			else if (e is Definition.BinaryExpression)
			{
				var e_ = e as Definition.BinaryExpression;
				e_.Left = ConvertTypeName(e_.Left);
				e_.Right = ConvertTypeName(e_.Right);
				return e_;
			}
			else if (e is Definition.PrefixUnaryExpression)
			{
				var e_ = e as Definition.PrefixUnaryExpression;
				e_.Expression = ConvertTypeName(e_.Expression);
				return e_;
			}
			else if (e is Definition.PostfixUnaryExpression)
			{
				var e_ = e as Definition.PostfixUnaryExpression;
				e_.Operand = ConvertTypeName(e_.Operand);
				return e_;
			}
			else if (e is Definition.BaseExpression)
			{
				return e;
			}
			else if (e is Definition.ObjectArrayCreationExpression)
			{
				var e_ = e as Definition.ObjectArrayCreationExpression;
				e_.Type = ConvertTypeName(e_.Type);
				e_.Args = e_.Args.Select(_ => ConvertTypeName(_)).ToArray();
				return e_;
			}
			else if (e is Definition.TypeExpression)
			{
				return e;
			}
			else if (e is Definition.InitializerExpression)
			{
				var e_ = e as Definition.InitializerExpression;
				e_.Expressions = e_.Expressions.Select(_ => ConvertTypeName(_)).ToArray();
				return e_;
			}

			throw new Exception();
		}

		Definition.TypeSpecifier ConvertTypeName(Definition.TypeSpecifier typeSpecifier)
		{
			if (typeSpecifier is Definition.SimpleType)
			{
				var src = typeSpecifier as Definition.SimpleType;
				var dst = new Definition.SimpleType();

				string srcType = GetTypeString(src.Namespace, src.TypeName);

				if (typeConverter.ContainsKey(srcType))
				{
					var dstType = typeConverter[srcType];
					dst.Namespace = dstType.Item1;
					dst.TypeName = dstType.Item2;
					dst.TypeKind = src.TypeKind;
				}
				else
				{
					dst.TypeName = src.TypeName;
					dst.Namespace = src.Namespace;
					dst.TypeKind = src.TypeKind;
				}

				return dst;
			}
			else if (typeSpecifier is Definition.ArrayType)
			{
				var src = typeSpecifier as Definition.ArrayType;
				var dst = new Definition.ArrayType();

				dst.BaseType = ConvertTypeName(src.BaseType) as Definition.SimpleType;

				return dst;
			}
			else if (typeSpecifier is Definition.GenericType)
			{
				var src = typeSpecifier as Definition.GenericType;
				var dst = new Definition.GenericType();

				dst.OuterType = ConvertTypeName(src.OuterType) as Definition.SimpleType;
				dst.InnerType = src.InnerType.Select(_ => ConvertTypeName(_)).OfType<Definition.TypeSpecifier>().ToList();

				return dst;
			}

			return typeSpecifier;
		}
		#endregion


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

		/// <summary>
		/// プロパティを関数に変換する編集コマンドを追加する。
		/// </summary>
		/// <param name="namespace_"></param>
		/// <param name="type_"></param>
		/// <param name="prop_"></param>
		/// <param name="method"></param>
		/// <returns></returns>
		public void AddEditFuncPropToMethodConverter(string namespace_, string type_, string prop_, string method)
		{
			Func<object, Tuple<bool, object>> func = (object o) =>
			{
				var mae = o as Definition.MemberAccessExpression;

				// プロパティが条件を満たさない
				if(mae?.Property?.Name != prop_)
				{
					return Tuple.Create<bool, object>(true, null);
				}

				if (mae?.Class?.Namespace == namespace_ && mae?.Class?.Name == type_)
				{
					// getter差し替え
					var invocation = new Definition.InvocationExpression();

					// 関数設定
					var memf = new Definition.MemberAccessExpression();
					memf.Method = new Definition.MethodDef();
					memf.Method.Name = method;
					memf.Expression = mae.Expression;
					invocation.Method = memf;

					// 引数設定
					invocation.Args = new Definition.Expression[0];

					return Tuple.Create<bool, object>(true, invocation);
				}

				return Tuple.Create<bool, object>(true, null);
			};

			AddEditFunc(func);
		}
	}
}

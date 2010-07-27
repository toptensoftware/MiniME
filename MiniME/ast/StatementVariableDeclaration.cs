using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementVariableDeclaration : Statement
	{
		public StatementVariableDeclaration()
		{
		}

		public void AddDeclaration(string Name, ast.ExpressionNode InitialValue)
		{
			var v = new Variable();
			v.Name = Name;
			v.InitialValue = InitialValue;
			Variables.Add(v);
		}

		public bool HasInitialValue()
		{
			foreach (var v in Variables)
			{
				if (v.InitialValue != null)
					return true;
			}
			return false;
		}


		public override void Dump(int indent)
		{
			foreach (var v in Variables)
			{
				if (v.InitialValue != null)
				{
					writeLine(indent, "variable `{0}`, initial value:", v.Name);
					v.InitialValue.Dump(indent + 1);
				}
				else
				{
					writeLine(indent, "variable `{0}`", v.Name);
				}
			}
		}


		public override bool Render(RenderContext dest)
		{
			dest.Append("var ");
			bool bFirst = true;
			foreach (var v in Variables)
			{
				if (!bFirst)
				{
					dest.Append(',');
				}
				else
				{
					bFirst = false;
				}

				dest.Append(dest.Symbols.GetObfuscatedSymbol(v.Name));
				if (v.InitialValue != null)
				{
					dest.Append("=");
					v.InitialValue.Render(dest);
				}
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var v in Variables)
			{
				if (v.InitialValue != null)
					v.InitialValue.Visit(visitor);
			}
		}


		public class Variable
		{
			public string Name;
			public ExpressionNode InitialValue;
		}

		public List<Variable> Variables=new List<Variable>();

	}
}

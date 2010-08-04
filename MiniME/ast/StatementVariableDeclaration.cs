using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a set of variable declaration.
	class StatementVariableDeclaration : Statement
	{
		// Constructor
		public StatementVariableDeclaration(Bookmark bookmark) : base(bookmark)
		{
		}

		// Add another variable declaration
		public void AddDeclaration(string Name, ast.ExpressionNode InitialValue)
		{
			var v = new Variable();
			v.Name = Name;
			v.InitialValue = InitialValue;
			Variables.Add(v);
		}

		// Check if any variables have an initial value
		// (used to check for invalid variable declaration in for-in loop)
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
			// Quit if all variables have been eliminated
			if (Variables.Count == 0)
				return false;

			// Statement
			dest.Append("var");

			// Comma separated variables
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

				// Variable name, possibly obfuscated
				dest.Append(dest.Symbols.GetObfuscatedSymbol(v.Name));

				// Optional initial value
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


		// Represents a single variable declaration
		public class Variable
		{
			public string Name;
			public ExpressionNode InitialValue;
		}

		public List<Variable> Variables=new List<Variable>();

	}
}

/*
 * MiniME
 * 
 * Copyright (C) 2010 Topten Software. Some Rights Reserved.
 * See http://toptensoftware.com/minime for licensing terms.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a function declaration
	class ExprNodeFunction : ExprNode
	{
		// Constructor
		public ExprNodeFunction(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public string Name;
		public List<Parameter> Parameters = new List<Parameter>();
		public CodeBlock Code;
		public string AssignedToName;

		public string Describe()
		{
			if (Name != null)
				return String.Format("function '{0}'", Name);

			if (AssignedToName != null)
				return String.Format("function '{0}'", AssignedToName);

			return "anonymous function";
		}


		public override string ToString()
		{
			return String.Format("function {0}", Name ?? "<anonymous>");
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "function `{0}`:", Name);
			foreach (var p in Parameters)
			{
				writeLine(indent + 1, p.ToString());
			}
			writeLine(indent, "body:");
			if (Code != null)
				Code.Dump(indent + 1);
			else
				writeLine(indent + 1, "<no implementation>");
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.function;
		}

		public override bool Render(RenderContext dest)
		{
			// Get obfuscated name before we enter our own scope
			string strObfuscatedName = dest.Symbols.GetObfuscatedSymbol(Name);

			// Enter a new symbol scope and tell symbol allocator
			// about our local symbols
			dest.EnterScope(Scope);

			// `function`
			dest.Append("function");

			// Function name not present for anonymous functions
			if (Name != null)
			{
				dest.Append(strObfuscatedName);
			}

			// Parameters
			dest.Append('(');
			for (int i = 0; i < Parameters.Count; i++)
			{
				if (i > 0)
					dest.Append(',');
				Parameters[i].Render(dest);
			}
			dest.Append(")");

			// Body of the function
			Code.Render(dest);

			// Clean up scope and we're finished
			dest.LeaveScope();
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var p in Parameters)
			{
				p.Visit(visitor);
			}
			Code.Visit(visitor);
		}

		public override ExprNode Simplify()
		{
			return this;
		}

		public override bool HasSideEffects()
		{
			return true;
		}

	}
}

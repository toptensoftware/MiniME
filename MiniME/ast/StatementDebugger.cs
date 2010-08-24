﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a return or throw statement
	class StatementDebugger : Statement
	{
		// Constructor 
		public StatementDebugger(Bookmark bookmark, Token op) : base(bookmark)
		{
			Op = op;
		}

		// Attributes
		public Token Op;

		public override void Dump(int indent)
		{
			writeLine(indent, Op.ToString());
		}

		public override bool Render(RenderContext dest)
		{
			if (Bookmark.warnings)
			{
				Console.WriteLine("{0}: warning: use of debugger statement", Bookmark);
			}

			dest.Append(Op.ToString().Substring(3));
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}

	}
}
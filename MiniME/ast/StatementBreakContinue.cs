using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a break or continue statement, with it's optional label
	class StatementBreakContinue : Statement
	{
		// Constructor
		public StatementBreakContinue(Bookmark bookmark, Token op, string label) : base(bookmark)
		{
			Op = op;
			Label = label;
		}

		// Attributes
		public Token Op;
		public string Label;

		public override void Dump(int indent)
		{
			writeLine(indent, "{0}:", Op.ToString());
			if (Label != null)
			{
				writeLine(indent + 1, "to label: `{0}`", Label);
			}
		}

		public override bool Render(RenderContext dest)
		{
			if (Op == Token.kw_fallthrough)
				return false;

			dest.DisableLineBreaks();
			dest.Append(Op.ToString().Substring(3));
			if (Label != null)
			{
				dest.Append(Label);
			}
			dest.EnableLineBreaks();
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}

		public override bool BreaksExecutionFlow()
		{
			return true;
		}
	}
}

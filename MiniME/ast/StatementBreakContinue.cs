using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementBreakContinue : Statement
	{
		public StatementBreakContinue(Token op, string label)
		{
			Op = op;
			Label = label;
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "{0}:", Op.ToString());
			if (Label != null)
			{
				writeLine(indent + 1, "to label: `{0}`", Label);
			}
		}

		public override bool Render(StringBuilder dest)
		{
			dest.Append(Op.ToString().Substring(3));
			if (Label != null)
			{
				dest.Append(' ');
				dest.Append(Label);
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}

		public Token Op;
		public string Label;
	}
}

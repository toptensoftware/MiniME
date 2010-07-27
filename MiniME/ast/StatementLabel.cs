using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementLabel : Statement
	{
		public StatementLabel(string label)
		{
			Label = label;
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "Label `{0}`:", Label);
		}
		public override bool Render(RenderContext dest)
		{
			dest.DisableLineBreaks();
			dest.Append(Label);
			dest.Append(':');
			dest.EnableLineBreaks();
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}



		public string Label;
	}
}

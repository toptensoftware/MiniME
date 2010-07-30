using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a preserved comment
	class StatementComment : Statement
	{
		// Constructor
		public StatementComment(string Comment)
		{
			this.Comment = Comment;
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "preserved comment");
		}

		public override bool Render(RenderContext dest)
		{
			dest.ForceLineBreak();
			dest.Append(Comment.Substring(0,2) + Comment.Substring(3));
			dest.ForceLineBreak();
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}

		public string Comment;


	}
}

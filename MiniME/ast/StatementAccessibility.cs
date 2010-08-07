using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a private statement
	class StatementAccessibility : Statement
	{
		// Constructor
		public StatementAccessibility(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public List<AccessibilitySpec> Specs=new List<AccessibilitySpec>();

		public override void Dump(int indent)
		{
			writeLine(indent, "accessibility:");
			foreach (var s in Specs)
			{
				writeLine(indent + 1, "`{0}`", s.ToString());
			}
		}

		public override bool Render(RenderContext dest)
		{
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}


	}
}

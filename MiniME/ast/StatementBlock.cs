using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementBlock : Statement
	{
		// Constructor
		public StatementBlock(Bookmark bookmark) : base(bookmark)
		{

		}

		// Attributes
		public List<Statement> Content = new List<Statement>();

		public override void Dump(int indent)
		{
			foreach (var n in Content)
			{
				n.Dump(indent);
			}
		}

		public override bool Render(RenderContext dest)
		{
			System.Diagnostics.Debug.Assert(false);
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			System.Diagnostics.Debug.Assert(false);
		}

		public void AddStatement(Statement stmt)
		{
			// Ignore if null (typical from extra semicolon in source)
			if (stmt == null)
				return;

			// Collapse child statement blocks into single block
			var block = stmt as StatementBlock;
			if (block != null)
			{
				foreach (var s in block.Content)
				{
					AddStatement(s);
				}
			}
			else
			{
				Content.Add(stmt);
			}
		}

	}
}

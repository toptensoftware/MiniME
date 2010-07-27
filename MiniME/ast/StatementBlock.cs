using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementBlock : Statement
	{
		public StatementBlock()
		{

		}

		public void RemoveRedundant()
		{
			for (int i=0; i<Content.Count; i++)
			{
				// Is this a statement block?
				if (Content[i].GetType() == typeof(StatementBlock))
				{
					// yes, take it's content an replace it here
					StatementBlock child = (StatementBlock)Content[i];
					Content.InsertRange(i + 1, child.Content);
					Content.RemoveAt(i);
					i--;
				}
			}
		}

		public override void Dump(int indent)
		{
			foreach (var n in Content)
			{
				n.Dump(indent);
			}
		}

		public override bool Render(RenderContext dest)
		{
			if (HasBraces)
			{
				dest.StartLine();
				dest.Append('{');
				dest.Indent();
			}

			bool bNeedSemicolon = false;
			for (var i=0; i<Content.Count; i++)
			{
				if (bNeedSemicolon)
					dest.Append(';');

				var s = Content[i];

				dest.StartLine();
				bNeedSemicolon=s.Render(dest);

				if (bNeedSemicolon && dest.Compiler.Formatted)
				{
					dest.Append(';');
					bNeedSemicolon = false;
				}
			}
			if (HasBraces)
			{
				dest.Unindent();
				dest.StartLine();
				dest.Append('}');
				return false;
			}

			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var c in Content)
			{
				c.Visit(visitor);
			}
		}

		public bool HasBraces = true;
		public List<Statement> Content=new List<Statement>();
	}
}

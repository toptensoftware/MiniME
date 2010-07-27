using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementVariableDeclarationMulti : Statement
	{
		public StatementVariableDeclarationMulti()
		{
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "Multiple variable declaration:");
			foreach (var v in Variables)
			{
				v.Dump(indent+1);
			}
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("var ");
			for (int i=0; i<Variables.Count; i++)
			{
				if (i > 0)
					dest.Append(",");
				Variables[i].RenderContent(dest);
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var v in Variables)
			{
				v.Visit(visitor);
			}
		}

		public List<StatementVariableDeclaration> Variables = new List<StatementVariableDeclaration>();
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class StatementVariableDeclaration : Statement
	{
		public StatementVariableDeclaration(string name)
		{
			Name = name;
		}

		public override void Dump(int indent)
		{
			if (InitialValue != null)
			{
				writeLine(indent, "variable `{0}`, initial value:", Name);
				InitialValue.Dump(indent + 1);
			}
			else
			{
				writeLine(indent, "variable `{0}`", Name);
			}
		}

		public override bool Render(StringBuilder dest)
		{
			dest.Append("var ");
			dest.Append(Name);
			if (InitialValue != null)
			{
				dest.Append("=");
				InitialValue.Render(dest);
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			if (InitialValue!=null)
				InitialValue.Visit(visitor);
		}

		public string Name;
		public ExpressionNode InitialValue;

	}
}

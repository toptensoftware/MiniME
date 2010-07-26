using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	class Parameter : Node
	{
		public Parameter(string name)
		{
			Name = name;
		}

		public string Name;

		public override string ToString()
		{
			return string.Format("parameter: {0}", Name);
		}

		public override void Dump(int indent)
		{
			write(indent, ToString());
		}
		public override bool Render(RenderContext dest)
		{
			dest.Append(dest.Symbols.GetObfuscatedSymbol(Name));
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}
	}
}

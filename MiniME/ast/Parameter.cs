using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a parameter to a function
	class Parameter : Node
	{
		// Constructor
		public Parameter(Bookmark bookmark, string name) : base(bookmark)
		{
			Name = name;
		}

		// Attributes
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Base class for all statement nodes
	// (wow this does alot!)
	abstract class Statement : Node
	{
		public Statement(Bookmark bookmark)
			: base(bookmark)
		{
		}

		public virtual bool BreaksExecutionFlow()
		{
			return false;
		}

		public virtual bool IsDeclarationOnly()
		{
			return false;
		}
	}
}

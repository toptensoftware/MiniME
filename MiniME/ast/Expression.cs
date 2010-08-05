/*
 * MiniME
 * 
 * Copyright (C) 2010 Topten Software. Some Rights Reserved.
 * See http://toptensoftware.com/minime for licensing terms.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Base class for all expression nodes
	class Expression : Node
	{
		// Constructor
		public Expression(ExprNode rootNode) : base(rootNode.Bookmark)
		{
			RootNode = rootNode;
		}

		public override void Dump(int indent)
		{
			RootNode.Dump(indent);
		}

		public override bool Render(RenderContext dest)
		{
			return RootNode.Render(dest);
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			RootNode.Visit(visitor);
		}

		public ExprNode RootNode;
	}

}

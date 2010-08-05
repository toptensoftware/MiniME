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
	// Represents a regular expression eg: /regex/gim
	class ExprNodeRegEx : ExprNode
	{
		// Constructor
		public ExprNodeRegEx(Bookmark bookmark, string re) : base(bookmark)
		{
			RegEx = re;
		}

		// Attributes
		string RegEx;

		public override string ToString()
		{
			return String.Format("regular expression : {0}", RegEx);
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "regular expression: {0}", RegEx);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append(RegEx);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}

	}

}

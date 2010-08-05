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
	// Represents array indexer (ie: square brackets)
	class ExprNodeIndexer : ExprNode
	{
		// Constructor
		public ExprNodeIndexer(Bookmark bookmark, ExprNode lhs, ExprNode index) : base(bookmark)
		{
			Lhs = lhs;
			Index = index; 
		}

		// Attributes
		public ExprNode Lhs;
		public ExprNode Index;

		public override void Dump(int indent)
		{
			writeLine(indent, "Index");
			Lhs.Dump(indent + 1);
			writeLine(indent, "with:");
			Index.Dump(indent + 1);
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		string GetIdentifier()
		{
			if (Index.GetType() != typeof(ast.ExprNodeLiteral))
				return null;

			object literal = ((ast.ExprNodeLiteral)Index).Value;
			if (literal.GetType() != typeof(string))
				return null;

			string identifier=(string)literal;
			if (!Tokenizer.IsIdentifier(identifier) && !Tokenizer.IsKeyword(identifier))
				return null;

			return identifier;
		}

		public override bool Render(RenderContext dest)
		{
			WrapAndRender(dest, Lhs, false);

			string str = GetIdentifier();
			if (str != null)
			{
				dest.Append('.');
				dest.Append(str);
			}
			else
			{
				dest.Append("[");
				Index.Render(dest);
				dest.Append("]");
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Lhs.Visit(visitor);
			Index.Visit(visitor);
		}

	}
}

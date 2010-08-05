using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// AST Visitor to create the SymbolScope object heirarchy
	//  - scopes are created for functions and catch clauses
	//  - also detects evil and marks scopes as such
	class VisitorSimplifyExpressions : ast.IVisitor
	{
		// Constructor
		public VisitorSimplifyExpressions()
		{
		}

		public bool OnEnterNode(MiniME.ast.Node n)
		{
			// Is it an expression?
			var expr = n as ast.Expression;
			if (expr==null)
				return true;

			// Simplify
			expr.RootNode = expr.RootNode.Simplify();


			return true;	// NB: Need to recurse into expressions in case it's a ExprNodeFunction which has a code block
							//		which will almost certainly contain deeper expressions that also could use simplification.

		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
		}
	}
}

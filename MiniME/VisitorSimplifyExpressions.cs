// 
//   MiniME - http://www.toptensoftware.com/minime
// 
//   The contents of this file are subject to the license terms as 
//	 specified at the web address above.
//  
//   Software distributed under the License is distributed on an 
//   "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
//   implied. See the License for the specific language governing
//   rights and limitations under the License.
// 
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
			if (expr != null)
			{
				// Simplify
				expr.RootNode = expr.RootNode.Simplify();
			}

			// Collapse statement blocks
			var statementBlock = n as ast.StatementBlock;
			if (statementBlock != null)
			{
				ast.StatementBlock.CollapseStatementBlocks(statementBlock.Content);
			}

			// Collapse code blocks
			var codeBlock = n as ast.CodeBlock;
			if (codeBlock != null)
			{
				ast.StatementBlock.CollapseStatementBlocks(codeBlock.Content);
			}

			return true;	// NB: Need to recurse into expressions in case it's a ExprNodeFunction which has a code block
							//		which will almost certainly contain deeper expressions that also could use simplification.

		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
		}
	}
}

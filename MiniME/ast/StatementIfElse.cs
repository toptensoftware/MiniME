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

namespace MiniME.ast
{
	// Represents an if-else statement
	class StatementIfElse : Statement
	{
		// Constructor
		public StatementIfElse(Bookmark bookmark, Expression condition) : base(bookmark)
		{
			Condition = condition;
		}

		// Attributes
		public Expression Condition;
		public CodeBlock TrueStatement;
		public CodeBlock FalseStatement;

		public override void Dump(int indent)
		{
			writeLine(indent, "if condition:");
			Condition.Dump(indent + 1);
			writeLine(indent, "true:");
			TrueStatement.Dump(indent + 1);
			if (FalseStatement != null)
			{
				writeLine(indent, "else:");
				FalseStatement.Dump(indent + 1);
			}

		}

		class CodeBlockFinder : IVisitor
		{
			public CodeBlock TrailingCodeBlock = null;

			public bool OnEnterNode(Node n)
			{
				var cb = n as CodeBlock;
				if (cb != null)
					TrailingCodeBlock = cb;

				return false;		// Don't recurve
			}

			public void OnLeaveNode(Node n)
			{
			}
		}

		public static CodeBlock GetTrailingCodeBlock(Statement s)
		{
			var cbf=new CodeBlockFinder();
			s.OnVisitChildNodes(cbf);
			return cbf.TrailingCodeBlock;

		}

		// Walk a code block and determine if the last statement is an
		// `if` statement without an `else` clause
		public static bool DoesCodeBlockHaveHangingElse(CodeBlock code)
		{
			// If the code block is going to render braces, we don't need to worry
			if (code.WillRenderBraces)
				return false;

			// Get the code block's last statement
			if (code.Content.Count == 0)
				return false;
			var stmt = code.Content[code.Content.Count - 1];

			// Is it an `if` statement
			var stmtIf = stmt as StatementIfElse;
			if (stmtIf != null)
			{
				if (stmtIf.FalseStatement == null)
					return true;
				else
					return DoesCodeBlockHaveHangingElse(stmtIf.FalseStatement);
			}

			// Check the last child code block of the last statement
			var TrailingCodeBlock = GetTrailingCodeBlock(stmt);
			if (TrailingCodeBlock != null)
			{
				// See if it has a hanging else
				return DoesCodeBlockHaveHangingElse(TrailingCodeBlock);
			}

			// Not trailing else
			return false;
		}

		public bool CheckForHangingElse()
		{
			// We don't have an else clause, so we're good.
			if (FalseStatement == null)
				return false;

			// Check if the True block has a hanging else
			return DoesCodeBlockHaveHangingElse(TrueStatement);
		}



		public override bool Render(RenderContext dest)
		{
			// Statement
			dest.Append("if(");
			Condition.Render(dest);
			dest.Append(")");

			// True block
			bool retv;
			if (CheckForHangingElse())
			{
				dest.StartLine();
				dest.Append("{");
				TrueStatement.RenderIndented(dest);
				dest.StartLine();
				dest.Append("}");
				retv = false;
			}
			else
			{
				retv = TrueStatement.RenderIndented(dest);
			}

			// False block
			if (FalseStatement != null)
			{
				if (retv)
					dest.Append(';');

				dest.StartLine();

				dest.Append("else");

				retv = FalseStatement.RenderIndented(dest);
			}

			return retv;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			Condition.Visit(visitor);
			TrueStatement.Visit(visitor);
			if (FalseStatement != null)
				FalseStatement.Visit(visitor);
		}

	}
}

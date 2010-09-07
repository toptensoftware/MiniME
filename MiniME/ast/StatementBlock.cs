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
	class StatementBlock : Statement
	{
		// Constructor
		public StatementBlock(Bookmark bookmark) : base(bookmark)
		{

		}

		// Attributes
		public List<Statement> Content = new List<Statement>();

		public override void Dump(int indent)
		{
			foreach (var n in Content)
			{
				n.Dump(indent);
			}
		}

		public override bool Render(RenderContext dest)
		{
			System.Diagnostics.Debug.Assert(false);
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var n in Content)
			{
				n.Visit(visitor);
			}
		}

		public void AddStatement(Statement stmt)
		{
			// Ignore if null (typically from extra semicolon in source)
			if (stmt == null)
				return;

			Content.Add(stmt);
		}

		public static void CollapseStatementBlocks(List<Statement> Statements)
		{
			for (int i = 0; i < Statements.Count; i++)
			{
				var block = Statements[i] as ast.StatementBlock;
				if (block != null)
				{
					// Remove the child block
					Statements.RemoveAt(i);

					// Insert child block statements in place
					for (int j=0; j<block.Content.Count; j++)
					{
						Statements.Insert(i+j, block.Content[j]);
					}

					// Rewind one since the first inserted child statement might
					// also be a statement block that needs to be collapsed
					i--;
				}
			}
		}
	}
}

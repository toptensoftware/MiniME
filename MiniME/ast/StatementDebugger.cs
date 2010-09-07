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
	// Represents a return or throw statement
	class StatementDebugger : Statement
	{
		// Constructor 
		public StatementDebugger(Bookmark bookmark, Token op) : base(bookmark)
		{
			Op = op;
		}

		// Attributes
		public Token Op;

		public override void Dump(int indent)
		{
			writeLine(indent, Op.ToString());
		}

		public override bool Render(RenderContext dest)
		{
			dest.Compiler.RecordWarning(Bookmark, "use of debugger statement");
			dest.Append(Op.ToString().Substring(3));
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}

	}
}

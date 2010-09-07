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
	class StatementReturnThrow : Statement
	{
		// Constructor 
		public StatementReturnThrow(Bookmark bookmark, Token op) : base(bookmark)
		{
			Op = op;
		}

		// Constructor
		public StatementReturnThrow(Bookmark bookmark, Token op, Expression value) : base(bookmark)
		{
			Op = op;
			Value = value;
		}

		// Attributes
		public Token Op;
		public Expression Value;

		public override void Dump(int indent)
		{
			writeLine(indent, Op.ToString());
			if (Value != null)
			{
				Value.Dump(indent + 1);
			}
		}

		public override bool Render(RenderContext dest)
		{
			if (Value == null)
			{
				dest.Append(Op.ToString().Substring(3));
				return true;
			}

	
			dest.DisableLineBreaks();
			dest.Append(Op.ToString().Substring(3));
			dest.EnableLineBreaksAfterNextWrite();
			Value.Render(dest);
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			if (Value!=null)
				Value.Visit(visitor);
		}

		public override bool BreaksExecutionFlow()
		{
			return true;
		}

	}
}

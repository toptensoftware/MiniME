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
	// Represents an array literal (eg: [1,2,3])
	class ExprNodeArrayLiteral : ExprNode
	{
		// Constructor
		public ExprNodeArrayLiteral(Bookmark bookmark) : base(bookmark)
		{

		}

		// Attributes
		public List<ExprNode> Values = new List<ExprNode>();

		public override string ToString()
		{
			return "<array literal>";
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "array literal:");
			foreach (var e in Values)
			{
				if (e == null)
					writeLine(indent + 1, "<undefined>");
				else
					e.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}


		public override bool Render(RenderContext dest)
		{
			dest.Append('[');
			bool bFirst = true;
			foreach (var e in Values)
			{
				if (!bFirst)
					dest.Append(",");
				else
					bFirst = false;
				if (e!=null)
					WrapAndRender(dest, e, false);
			}
			dest.Append(']');
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var e in Values)
			{
				if (e!=null)
					e.Visit(visitor);
			}
		}


		public override ExprNode Simplify()
		{
			SimplifyList(Values);
			return this;
		}

		public override bool HasSideEffects()
		{
			return false;
		}

	}
}

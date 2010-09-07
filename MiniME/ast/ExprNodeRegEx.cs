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


		public override ExprNode Simplify()
		{
			return this;
		}

		public override bool HasSideEffects()
		{
			return false;
		}

	}

}

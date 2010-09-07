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
	// Represents a root level symbol, or a member on the rhs of a member dot.
	class ExprNodeIdentifier : ExprNode
	{
		public ExprNodeIdentifier(Bookmark bookmark) : base(bookmark)
		{

		}
		// Constructor
		public ExprNodeIdentifier(Bookmark bookmark, string name) : base(bookmark)
		{
			Name = name;
		}

		// Constructor
		public ExprNodeIdentifier(Bookmark bookmark, string name, ExprNode lhs) : base(bookmark)
		{
			Name = name;
			Lhs = lhs;
		}

		// Attributes
		public string Name;
		public ExprNode Lhs;

		public override void Dump(int indent)
		{
			if (Lhs == null)
				writeLine(indent, "Variable `{0}`", Name);
			else
			{
				writeLine(indent, "Member `{0}` on:", Name);
				Lhs.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			if (Lhs != null)
			{
				WrapAndRender(dest, Lhs, false);
				dest.Append(".");
				dest.Append(dest.Members.GetObfuscatedSymbol(Name));
			}
			else
			{
				// Find the symbol and check if it's a constant
				var s = dest.CurrentScope.FindSymbol(Name);
				if (s != null && s.ConstValue != null)
				{
					ExprNodeLiteral.RenderValue(dest, s.ConstValue);
				}
				else
				{
					dest.Append(dest.Symbols.GetObfuscatedSymbol(Name));
				}
			}
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			if (Lhs!=null)
				Lhs.Visit(visitor);
		}

		public override ExprNode Simplify()
		{
			if (Lhs != null)
			{
				Lhs = Lhs.Simplify();
			}
			return this;
		}

		public override bool HasSideEffects()
		{
			return false;
		}

	}
}

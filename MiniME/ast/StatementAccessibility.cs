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
	// Represents a private statement
	class StatementAccessibility : Statement
	{
		// Constructor
		public StatementAccessibility(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		public List<AccessibilitySpec> Specs=new List<AccessibilitySpec>();

		public override void Dump(int indent)
		{
			writeLine(indent, "accessibility:");
			foreach (var s in Specs)
			{
				writeLine(indent + 1, "`{0}`", s.ToString());
			}
		}

		public override bool Render(RenderContext dest)
		{
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}


	}
}

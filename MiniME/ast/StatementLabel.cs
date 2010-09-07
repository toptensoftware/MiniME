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
	// Represents a label statement
	class StatementLabel : Statement
	{
		// Constructor
		public StatementLabel(Bookmark bookmark, string label) : base(bookmark)
		{
			Label = label;
		}

		// Attributes
		public string Label;

		public override void Dump(int indent)
		{
			writeLine(indent, "Label `{0}`:", Label);
		}
		public override bool Render(RenderContext dest)
		{
			dest.DisableLineBreaks();
			dest.Append(Label);
			dest.Append(':');
			dest.EnableLineBreaks();
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}
	}
}

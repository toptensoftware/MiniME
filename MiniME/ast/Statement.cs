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
	// Base class for all statement nodes
	// (wow this does alot!)
	abstract class Statement : Node
	{
		public Statement(Bookmark bookmark)
			: base(bookmark)
		{
		}

		public virtual bool BreaksExecutionFlow()
		{
			return false;
		}

		public virtual bool IsDeclarationOnly()
		{
			return false;
		}
	}
}

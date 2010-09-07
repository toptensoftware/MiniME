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

namespace MiniME
{
	// All tokenize/parse/render errors thrown through this
	public class CompileError : Exception
	{
		// Constructor
		internal CompileError(string message, Tokenizer t)
		{
			m_strMessage = message;
			m_Bookmark = t.GetBookmark();
		}

		// Attributes
		string m_strMessage;
		Bookmark m_Bookmark;

		public override string Message
		{
			get
			{
				return string.Format("{0}: {1}", m_Bookmark.ToString(), m_strMessage);
			}
		}

	}
}

# MiniME - Preserved Comments

Normally, you'll want MiniME to strip out all comments from your JavaScript files. Occassionally
however there will be comments that should be left in - in particular copyright messages and 
credits.

To instruct MiniME to keep a particular comment, simply place and exclamation point as the first
character in the comment, for example

	{{JavaScript}}
	//! Copyright (C) 2010 Topten Software.  Some Rights Reserved
	
It also works with C-style comments:

	{{JavaScript}}
	/*! 
		Copyright (C) 2010 Topten Software.  
		Some Rights Reserved
	*/
	
After minification, the comments will be preserved and the exclamation point be removed.

	   
See also [In-script Directives](usage-directives).

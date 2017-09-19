# MiniME - Multiple File Support

MiniME supports two techniques for combining a set of script files into a single minimized
library.

* Multiple scripts on the command line
* Using `include` directives to combine multiple scripts into a single module.

You can also use a combination of both techniques.

### Multiple Scripts on the Command line ###

The easiest way to combine multiple scripts is by specifying each as a separate command
line argument, for example:

	mm script1.js script2.js script3.js -o:MyScriptLibrary.min.js
	
Using this technique, each script is processed as a separate unit - so privatized declarations
in one script aren't accessible to the other scripts.  Only non-obfsucated declarations will
work across scripts.  

Use this technique if you're simply combining a set of unrelated scripts.

### Using `include` Directives ###
 
MiniME's `include` directive allows one script file to include another.  For example suppose
MyScriptLibrary.js contains:

	{{javascript}}
	// include:script1.js
	// include:script2.js
	// include:script3.js
	
To minify:

	mm MyScriptLibrary.js -o:MyScriptLibrary.min.js
	
In this example, obfuscated symbols in each of the input scripts will be accessible from each
of the other scripts.

You can use the include directive in any .js file, but usually it will be used on it's own 
in a single file to declare a JavaScript library - a minified set of related scripts.


See also [Command Line](usage.md) and [API](usage-api.md).

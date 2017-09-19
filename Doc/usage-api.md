# MiniME API

In addition to the command line tool, MiniME can also be invoked programmatically through
a programming interface (API) from any .NET client program.  This can be used to dynamically
obfuscate JavaScript files directly on an ASP.NET server.

1. Before using MiniME programmatically, you must add a reference to it from your .NET project.  
Depending on your development environment this might be done in any of a number of different ways - 
please consult the documentation for your development tool.

2. Create an instance of the MiniMI Compiler object.  eg:

	```C#
	// Create an instance of the MiniME minifier
	var mm = new MiniME.Compiler();
	```
    
3. Set MiniME options. eg:

	```C#
	// Setup options
	mm.MaxLineLength = 0;
	```
	
4. Add files to process:

	```C#
	// Add files
	mm.AddFile("MyJavaScriptFile.js", Encoding.UTF8)
	```
		
	or, add script directly
	
	```C#
	mm.AddScript("snippet", "<javascript code here>");
	```
		
5. Compile the script

	```C#
	// Compile and write to an output file
	mm.OutputFileName="MyMinifiedFile.js";
	mm.OutputEncoding=Encoding.UTF8;
	mm.Compile();
	```
		
	or, compile to a string
	
	```C#
	var ObfuscatedScript=mm.CompileToString();
	```
		
6. That's it!


See also [Command Line](usage.md) and [MiniME In-script Directives](usage-directives.md).

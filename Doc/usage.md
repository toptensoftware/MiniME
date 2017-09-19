# MiniME - Usage

Once you've downloaded MiniME, you'll need to extract the binaries to somewhere on your 
path, or update your path to include the folder containing the MiniME files.
Typically MiniME should be called from your build process, but it can also be run directly 
from a command prompt or terminal.

MiniME runs under Windows with .NET 3.5 installed.  It has also been know to run 
under Mono on Linux and Mac OS X - though this has not been tested extensively.

## Invoking MiniME ##

To run MiniME simply pass the name of the JavaScript file (or files) you want to process:

	mm MyJavaScriptFile.js
	
This will produce a file named `MyJavaScriptFile.min.js` in the same folder as the input file.

For Linux/Mac users with Mono installed use the following command format:

	mono mm.exe MyJavaScriptFile.js


## Command Line Reference ##

MiniME supports the following command line arguments:

`-o:<outputfile>`
: Sets the name of the output file

`-stdout`
: Send the output to stdout rather than a file.

`-linelen:<characters`
: Sets the maximum line length.  By default, MiniME inserts line breaks every 120 characters.  Use 
this option to override this behaviour.  Specify `0` for no line breaks.

`-no-obfuscate`
: Disable all obfuscation - just remove whitespace, comments etc...

`-ive-donated`
: Prevents MiniME from inserting a small credit comment mentioning itself.

`-inputencoding`
: Specifies the character encoding of any input files that follow on the command line.

`-outputencoding`
: Specifies the character encoding of the output file.  If not specified, the same character encoding
of the first input file is used.

`-listencodings`
: Generates a list of available character encoding names.

`-D:<directory>`
: Sets the current directory to `<directory>`, which will be used for any subsequent unqualified command line path arguments.

`-check-filetimes`
: Checks the timestamp of all input files (including response fles) and only generates the output
file if one or more input file has changed.  This option also creates a file named `<outputfile>.minime-options`
that stores the options used. For `-check-filetimes` to work, the same set of command line
arguments must be specified.

`-no-options-file`
: Don't use or create the options file normally created by `-check-filetimes`.

`-no-warnings`
: Don't display code quality warnings for any subsequent files on the command line

`-warnings`
: Turn code quality warnings back on for subsequent files.

`-css`
: Forces CSS compression instead of Javascript minification.  Only required if using unusual file extensions.

`-js`
: Forces Javascript minification over CSS compression. Only required if using unusual file extensions.

`@<responsefile>`
: Load command line arguments from a file name `<responsefile>`.  While processing the arguments 
in the response file, the current directory is changed to the directory of the response file.

`-h` or `-?`
: Display command line help

`-v`
: Display the MiniME version header and quit.

`-nologo`
: Don't show the MiniME logo.  Use with `-stdout` to get a clean output.

## Diagnostics Options ##

The following options are intended for developers working on MiniME, but might also
be useful during general usage.

`-diag-formatted`
: Format the output to make it more readable (though probably not as readable as the
original source code - unless you're a really messy coder!).  Possibly useful for 
de-obfuscating previously obfuscated scripts - good luck with that!

`-diag-symbols`
: Displays symbol allocation information in the generated output.

`-diag-ast`
: Dumps the entire parsed abstract syntax tree to stdout.

`-diag-scopes`
: Dumps information about all function scopes to stdout.



See also [API](usage-api.md) and [In-script Directives](usage-directives.md).

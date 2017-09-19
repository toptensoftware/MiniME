# MiniME - Implementation Notes

This page describes important information for anyone interested in working on the MiniME code base.

## Code Repository ##

The source code for MiniME is available from GitHub:

* [Source Code](http://github.com/toptensoftware/MiniME)

## Development Environment ##

To build MiniME you will need:

* Visual Studio 2008, with C# installed.
* [NUnit][] for running unit tests.
* [Info-zip] command line utility.

[NUnit]: http://www.nunit.org
[Info-zip]: http://www.info-zip.org

## Unit Tests ##

To run the unit tests, edit the project settings for the MiniMETestCases project and set the following:

* Start External Program - `C:\Program Files (x86)\NUnit 2.5.5\bin\net-2.0\nunit.exe` (or equivalent)
* Command line arguments - `MiniMETestCases.dll /run`

Each unit test is defined as a text file resource that uses lines of hyphens to delimit the end of
the input script from the expected output script, for example:

	// This is a test scipt
	function fn()
	{
	}
	-----
	function fn(){}
	-----
	
Comments about the test can be inserted as regular JavaScript comments in the input text area.  

Some MiniME options can be set using a JavaScript comment with the option name surrounded in 
square brackets - see the included test scripts for examples.



## How it Works ##

In processing a JavaScript file, MiniME does the following:

1. Loads the file into a `StringScanner`
2. Creates a `Tokenizer` that reads characters from the `StringScanner`
3. Creates a `Parser` that reads tokens from the `Tokenizer` and generates 
	a complete Abstract Syntax Tree.
4. Runs the `VisitorScopeBuilder` visitor over the AST and creates a heirarchy of 
	`SymbolScopes` and connects each to it's defining function.
5. Runs the `VisitorCombineVarDecls` visitor to combine consecutive variable declarations
	into a single statement.
6. Runs the `VisitorSymbolDeclaration` visitor to find all symbol and member declarations
	and populates each `SymbolScope` with the symbols and members it defines.
7. Runs the three `VisitorConstDetectorPass[1,2,3]` visitors to detect constant symbol 
	declarations, eliminate symbols that are subsequent modified (and therefore not const)
	and finally remove symbols that are determined to be constant.
8. Runs the `VisitorSimplifyExpressions` visitor that instructs all expressions to attempt
	to simplify themselves.
9. Runs the `VisitorSymbolUsage` visitor to calculate the usage frequency of all symbols
	and members.
10. Prepares all SymbolScopes by sorting used symbols by frequency and determining which
	symbols can be obfuscated.
11. Renders the AST, building the final output script.





## Terms ##

In this documentation and the MiniME codebase itself, the following terms are used:

Abstract Syntax Tree (AST)
:	A heirarchial representation of the entire structure of a JavaScript file as a set
	of objects.

Accesibility
:	Whether a symbol is private or public.

Compile
:	The process of tokenizing and parsing a JavaScript file into an Abstract Syntax Tree, 
	optionally performing a set of transformations and then re-rendering the AST as a new
	JavaScript file.
	
	Obviously this is not a true definition of what it means to Compile code, but explains 
	how it is used in the context of MiniME.

Frequency
:	The number of times a Symbol or Member is used - used to allocate shorter names to 
	more frequently used symbols.

Member
:	Any symbol on the right-hand side of a period. ie: object properties and methods.

Parse
:	The process of reading a stream of tokens and producing an AST.

Rank
:	The position of a symbol in a list of symbols sorted by frequency.

Render
:	The process of generating JavaScript code from an AST.

Scope
:	Either the global scope of a JavaScript file, or the scope of a function body.

Symbol
:	Any symbol, not on the right-hand side of a period. ie: parameters, local variables, 
	global variables and non-anonymous function names.

Token
:	A single element in a JavaScript file, eg: an operator, identifier, keyword, string
	ornumber literal etc...

Tokenize
:	The process of reading a stream of characters and generating a stream of tokens.

Visitor
:	A object the enumerates the entire AST inpecting each node.


## Project Overview ##

The MiniME codebase is contained within a single Visual Studio 2008 solution with the 
following projects:

MiniME
: The main MineME.dll assembly that contains all the code of interest.

MiniMETestCases
: NUnit test cases 

mm
: The command line console application, which serves as a simple front end for MiniME.dll

Only the MiniME project is discussed in any detail here.


## Class Overview ##

The following is a brief description of each of the classes in the MiniME project:


### `MiniME` namespace ### 

`AccessibilitySpec` class
:	Stores a symbol specifier for a `private` or `public` directive with methods to parse
	and match the specifier against an `ast.ExprNodeIdentifier`.

`CommandLine` class
:	Utility class for processing command line arguments and helpers for displaying the 
	command line logo and help. 

	This functionality is included in the main assembly
	because it was thought the ability to process response files through the API might be 
	useful in a server environment.

`CompileError` class
:	Exception class thrown when errors in processing are encountered.  Typically stores
	an error message and a `Bookmark` reference that points to the offending JavaScript code.

`Compiler` class
:	The main MiniME compiler class - stores options, loads JavaScript files, processes all 
	files, writes output files, checks file times.

`Parser` class
:	Reads tokens from a `Tokenizer` and builds an Abstract Syntax Tree for a JavaScript program.

`RenderContext` class
:	Provides context for rendering the final output, including storing the current function
	scope, providing references to symbol and member name allocators and providing methods for
	rendering the actual output text.

`StringScanner` class
:	Stores a current position in a string being parsed.

`Symbol` class
:	Stores information about a symbol including it's scope, frequency, rank and accessibility.

`SymbolAllocator` class
:	Stores a mapping of original symbol names to obfuscated names.  Internally manages its own
	concept of scope and is responsible for allocating symbols according to frequency of use.

`SymbolFrequency` class
:	Stores information about the frequency of a set of symbols and provides methods to sort
	accordingly.

`SymbolScope` class
:	Represents a global or function scope and stores information about all symbols declared 
	and used in that scope.

`TextFileUtils` class
:	Helpers for working with file encodings.

`Tokenizer` class
:	Reads characters from a `StringScanner` and produces a stream of tokens for the `Parser`.
	
	The Tokenizer is also responsible for processing `include` directives and producing a 
	continuous sequence of tokens to the parser.

`Utils` class
:	Miscellanous utility functions

`VisitorCombineVarDecls` class
:	Visitor to merge consecutive `var` declarations into a single statement.

`VisitorConstDetectorPass`*N* class
:	Three pass visitors to detect constant declarations.

`VisitorScopeBuilder` class
:	Allocates a `SymbolScope` for each function and connects scopes into a heirarchy.

`VisitorSimplifyExpressions` class
:	Visits all expressions and requests they simplify themselves.

`VisitorSymbolDeclaration` class
:	Finds symbol declarations and registers them with the enclosing symbol scope.

`VisitorSymbolUsage` class
:	Updates the frequency count of symbols.



### `MiniME.ast` namespace ### 

The `ast` namespace encapsulates all the classes used to store the Abtract Syntax Tree
(AST) of the parsed input file(s).

`ast.Node` class
:	Base class for all AST elements.

`ast.ExprNode` class
:	Base class for all expression nodes.

`ast.ExprNode`* classes
:	Implementations of each of the expression node types.

`ast.Expression` class
:	Holds the root node of an expression.

`ast.CodeBlock` class
:	Holds a sequence of statements that define a code block.  Code blocks may or may not
	have surround braces depending on context.  (eg: the code block for the root scope of a file
	doesn't have braces, the code block for a try statement always does and the code block of
	a while statment will if there is more than one contained statement)

`ast.Parameter` class
:	Represents a parameter to a function.

`ast.Statement` class
:	Base class for all statements. A statement is anything that can appear in a CodeBlock.

`ast.Statement`* classes
:	Implementations of each of the statement types.

`ast.StatementExpression` class
:	Deserves special mention,  is used to store an expression as a statement - simply wraps 
	up an ast.Expression allowing it to be stored in a code block.




# MiniME - Code Quality (Lint)

In additional to minifying and obfuscating your scripts, MiniME can check for 
common JavaScript programming errors and suspicious looking code.

## Enabling and Disabling Code Warnings ##

Rather than a complicated set of options to control what's checked, MiniME's code 
checking is either on or off.  Code checking is on by default, but can be disabled
with the command line option `-no-warnings`.  The `-no-warnings` switch will disable
code checking for all subsequent files on the command line.  Similarly,
the `-warnings` switch can be used to re-enable code checking for other files. eg:

	mm -no-warnings DontCheckMe.js -warnings CheckMe.js
	
You can also control warnings within your script files with the `// warnings:` directive.

`// warnings:on`
: Turn warnings on

`// warnings:off`
: Turn warnings off

`// warnings:push:on`
: Save the current warning state before turning warnings on
	
`// warnings:push:off`
: Save the current warning state before turning warnings off

`// warnings:pop`
: Restore the last saved warning state

So for example:

	{{JavaScript}}
	// warnings:push:off
	eval("performEvil()")
	// warnings:pop
	

## What does MiniME Check For? ##

MiniME aims to provide a reasonable set of warnings for common, realistic programming 
errors.  It tries to be helpful, doesn't suffer from OCD and won't impose weird 
coding conventions upon you.

The following explains all the code quality warnings that MiniME can generate.

### Unreachable Code ###

Unreachable code is any code that follows a `return`, `throw`, `break` or `continue` 
statement.  For example:

	{{JavaScript}}
	function MyFunction()
	{
		return;
		alert("Can't get here");	// Unreachable code
	}

### Use of debugger Statement ###

The JavaScript debugger statement shouldn't be used in production code, so MiniME
let's you know if you've accidentally left one in.

### Expression Has No Side Effects ###

Code that has no side effects is redundant and can often indicate an error.  For
example:

	{{JavaScript}}
	$this.bind('keydown',function (e) {
		e.preventDefault;		// This is valid, but wrong - should be e.preventDefault()
		return false;
	});

### Execution Falls Through To Next Case ###

A common programming error is forgetting to put a `break` statement between each
`case` clause in a `switch` statement.  MiniME will warn of this:

	{{JavaScript}}
	switch (x)
	{
		case 1:
			alert("I'm falling");
									// Shouldn't there be a break or return here?
		
		case 2:
			alert("I'll catch you");
			break;
	}
	

If you intend for execution to fall through, you can suppress this warning with a comment
that says "fall through".  eg:

	{{JavaScript}}
	switch (x)
	{
		case 1:
			alert("I'm falling");
			
			// fall through
		
		case 2:
			alert("I'll catch you");
			break;
	}
	
	
### Trailing Comma in Array or Object Literal ###

Not all browsers support trailing commas in object and array literals.  MiniME warns about this:

	{{JavaScript}}
	var x={"Apples":"Red", "Bananas":"Yellow", };	// Warning
	var y=["Apples", "Pears", ];					// Warning
	

Missing elements within array literals _are_ allowed:
	
	{{JavaScript}}
	var z=[1,2,,,,4];		// This is OK
	
### Use of Composite Expression ###

Composite expressions are expressions involving the comma operator.  These are generally confusing
and usually not required so MiniME lets you know:

	{{JavaScript}}
	// This is confusing:
	prevOffsetParent = offsetParent, offsetParent = elem.offsetParent;
	
Since composite expressions *are* useful in `for` loops, MiniME doesn't warn in that case.

	{{JavaScript}}
	// This is OK
	for (x=0, y=0; x<10; x++, y++)
	{
	}
	
### Unnecessary Semicolon ###

JavaScript will tolerate extra semicolons but these can often be a sign of some other problem. 

MiniME generates a warning for cases like this:

	{{JavaScript}}
	while (SomeCondition());		// Extra semicolon
	
If you really want an empty loop, do this instead:

	{{JavaScript}}
	while (SomeCondition())	{}
	
### Missing Semicolon ###

JavaScript will tolerate missing semicolons but MiniME will warn you:

	{{JavaScript}}
	var x="Hello"			// Warning
	var y="World";			// No warning
	
### Assignment as Condition of Flow Control Statement ###

MiniME warns of assignments in the condition part of a flow control statement.  eg:

	{{JavaScript}}
	if (x=y)		// Really? Shouldn't this be x==y
	{
		DoSomething();
	}
	
To disable this assignment, use an extra set of parentheses (which MiniME will remove during
minification anyway):

	{{JavaScript}}
	if ((x=y))		// Oh! You really mean assignment.  OK.
	{
		DoSomething();
	}
	
### Symbol Has Multiple Declarations ###

JavaScript tolerates a variable or function being declared multiple times, but this can easily
lead to a single variable where you intended two.  MiniME warns about this.

	{{JavaScript}}
	function DoSomething()
	{
		var i=23;
		
		// and then later in the same function
		
		var i=42;		// Symbol declared multiple times
	}
	
### Use of `new Array()` with One Argument Creates a Sized Array ###

Calling `new Array()` with a single argument like this:

	{{JavaScript}}
	var x=new Array(10);
	
creates a sized array - in this case an array of 10 elements.  

Calling `new Array()` with zero or more than one argument creates an array with 
initialized elements.  So this:

	{{JavaScript}}
	var x=new Array(10,20)
	
creates an array with two elements 10 and 20.

This is fundamentally unintuitive so MiniME generates a warning whenever is sees
`new Array` with a single argument.  If you really want to create a sized array
you can suppress the warning with a directive.

	{{JavaScript}}
	// warnings:push:off
	var x=new Array(10);
	// warnings:pop

Note that during minification MiniME will replace calls to `new Array()` with an array literal `[]`
and `new Object()` with an object literal `{}`.  Calls to `new Array` with one argument are left alone.


### Symbol Used Outside Declaring Pseudo Scope ###

Although JavaScript allows braced statement blocks, these don't actually open a new
symbol scope. This is unlike most other C-style programming languages and is easy forget 
or be unaware of.

MiniME lets you code as if JavaScript does support scope in statement blocks and warns you if you use 
a symbol outside the so-called "scope" (aka "pseudo-scope") in which it was declared.

The following would yield this warning:

	{{JavaScript}}
	if (HappyToday())
	{
		var msg="Hello World";
	}	
	else
	{
		var msg="Ugh";
	}
	alert(msg);

To fix the warning, declare the variable outside the inner scope like this:

	{{JavaScript}}
	var msg;
	if (HappyToday())
	{
		msg="Hello World";
	}	
	else
	{
		msg="Ugh";
	}
	alert(msg);


MiniME considers the variables declared in a `for` statement to belong to the scope of
the statement and warns if they're used after the loop.

	{{JavaScript}}
	for (var i=0; i<10; i++)
	{
		// Do something
	}
	
	alert(i);		// Sorry, `i` was in the pseudo-scope of the for-loop.

Similary, to resolve this move the variable declaration:

	{{JavaScript}}
	var i;
	for (i=0; i<10; i++)
	{
		// Do something
	}
	
	alert(i);		// Now MiniME is happy.


Note that although this approach does provide a model closer to what most programmers
are used to, it's not perfect.  In particular, variables declared in an inner scope
may have an unexpected initial value.

eg:

	{{JavaScript}}
	if (SomeCondition())
	{
		var x=23;
	}
	
	if (SomeOtherCondition())
	{
		var x;
		alert(x);		// x may have the value 23 from above, not undefined.
	}

MiniME provides no warning in this case.

### Use of `with` and Use of `eval` ###

The `with` and `eval` statements are generally considered bad form - some even say "evil". 

These statements not only prevent MiniME from being able to effectively obfuscate any containing 
scopes they can also adversely affect the performance of the just-in-time compiling JavaScript 
engines in most modern browsers.

If you must use these, try to move them to a function at the global scope to allow obfuscation
of more deeply nested scopes.


### Symbol Hides Previous Declaration ###

Re-using symbols is generally considered bad practice.  MiniME will warn about the following:

	{{JavaScript}}
	function DoSomething(x)
	{
		for (var x=0; x<10; x++)	// New variable x hides parameter x
		{	
			alert(x);				// Which one?
		}
	}

### Assignment to Undeclared Variable ###

Assigning to an undeclared variable creates a new global variable which is generally not the intent. 

To prevent this warning, instead of this:

	{{JavaScript}}
	global_variable=42;

declare the variable:

	{{JavaScript}}
	var global_variable=42;
	
or, use a MiniME directive like this:

	{{JavaScript}}
	// public:global_variable
	global_variable=42;

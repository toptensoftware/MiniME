# MiniME - Directives

By default MiniME takes the low risk approach of only obfuscating symbols it knows 
can't be used externally.  This includes local variables, parameter names and nested
function names.

Often however, there are additional symbols that are internal to your script that 
don't need to be publicly accessible.  In these cases, MiniME requires you to explicitly 
opt-in if you want those symbols obfuscated.

Directives are comments in your JavaScript that provide additional information to MiniME
about what symbols can be obfuscated. These directives follow the general form:

	// private:<symbolspecifier>
	// public:<symbolspecifier>

The follow sections provide examples of symbol specifiers.

Note: This section covers directives for controlling obfuscation.  There are also directives 
for [comment preservation](usage-comments.md) and for controlling [lint warnings](lint.md).


### Obfuscating a Specific Global Scope Symbol ###

By default, MiniME doesn't obfuscate variable or function declarations at the global scope.
You can change this behaviour with the `private` directive:

	{{javascript}}
	// private:my_internal_var
	var my_internal_var;

This directive also works inside a function scopes for controlling the accessibility 
of local variables, parameters and nested functions (though generally it's not needed).

### Obfuscating a set of Global Scope Symbols with Wildcards ###

A wildcard symbol specifier allows selection of a set of symbols:

	{{javascript}}
	// private:internal_*
	var internal_var;				// Obfuscated
	function internal_function(){}	// Obfuscated
	function other(){}				// Not obfuscated
	
For a symbol to be enabled for obfuscation when using wildcards, it must be declared
in the current scope, using either `var` or `function`.  Unknown symbols, or symbols 
declared in inner scopes won't be selected for obfuscation by wildcard directives.

	{{javascript}}
	// private:internal_*
	alert(internal_not_declared)	// internal_not_declared won't be obfuscated
									// because it's not declared with a var statement

Wildcards support "`*`" to match any sequence of characters and "`?`" to match any character.  For
more complex symbol matching, regular expressions can be used by surrounding the expression with
forward slashes "`/`".  eg:

	{{javascript}}
	// private:/^internal_.*$/
	
	
### Obfuscating Properties and Methods ###

By default, MiniME never obfuscates object property or method names (ie: "members"). MiniME 
doesn't attempt or pretend to understand object types and as such never obfuscates object 
members - unless you use directives to explicitly declare those that are internal and are 
therefore safe for obfuscation.

The simplest way to declare a member for obfuscation is with a `private` directive
where the symbol specifier is prefixed by a period.

For example:

	{{javascript}}
	// private:.m_internal
	
    function MyObject()
    {
		this.m_internal=23;		// This will be obfuscated
    }

Wildcards and regular expressions are also allowed, for example:

	{{javascript}}
	// private:.m_*
	
    function MyObject()
    {
		this.m_x=23;		// All references to 'm_x' will be obfuscated
		this.m_y=99;		// and 'm_y'
    }

For a symbol to be selected for obfuscation by wildcard member directives, it must appear
on the left hand side of an assignment somewhere in the containing scope, for example:

	{{javascript}}
	// private:.m_*
	
	var x={};
	x.m_x=23;			// m_x selected for obfuscation
	alert(other.m_x);	// m_x is obfuscated (all references to m_x after a period are obfuscated)
	alert(other.m_y);	// m_y not obfuscated (no assignment to select it)

Members can also be selected for obfuscation by assignment of an object literal, for example:

	{{javascript}}
	// private:.m_*
	var x=
	{
		m_x:23,			// both of these will be obfuscated
		m_y:24
	};
	
	
### Obfuscating Members of a Target ###

You can select for all members of a target symbol to be obfuscated with a `private` directive where
the specifier is suffixed with a period, for example:

	{{javascript}}
	// private:priv.
	var priv={};
	
	// Both x and y will be obfuscated
	priv.x=23;
	priv.y=23;	
	
This is a handy way to declare a set of private prototype members, for example:

	{{javascript}}
	function MyObject()
	{
	}
	
	// private:priv.
	var priv=MyObject.prototype;
	
	// Both Method1 and Method2 will be obfuscated
	priv.Method1=function(){}
	priv.Method2=function(){}
	
	// Method 3 wont be obfuscated
	MyObject.prototype.Method3=function(){}
	

### Scope of Directives ####	

Directives only apply within the containing scope in which they're declared. Using the 
same directive in two different scopes will probably result in the same symbol being
allocated a different obfuscated symbols in each scope.

	{{javascript}}
	function Closure1()
	{
		// private:.m_*
		var obj={m_x=23, m_y=24};
	}
	
	function Closure2()
	{
		// private:.m_*
		
		// These will possibly (probably) be allocated
		// different symbols that those in Closure1()
		var obj={m_x=23, m_y=24};
	}
	
If Closure1 and Closure2 above need to share private variables, move the directive to 
the next outer scope (ie:global scope).
	
	
### Order of Directives ###

When a symbol matches multiple wildcard directives, the order of the directives is 
important with the most recent directive applied first.  For example:

	{{javascript}}
	// private:.m_*
	// public:.m_pub*
	var x={m_private:23, m_public:24};	// Only m_private will be obfuscated

Non-wildcard directives are always apply first, so in this example the order of
m_public is irrelevant:

	{{javascript}}
	// public:.m_public
	// private:.m_*
	var x={m_private:23, m_public:24};	// Only m_private will be obfuscated


	
### Final Notes ###

Using the above directives it is fairly easy to declare private member properties
and methods.  The main thing to remember is that once a member has been selected
for obfuscation, all references to it will be obfuscated - regardless of what's on the
left-hand side of the period.

	{{javascript}}
	// private:priv.
	var priv={};
	priv.x=23;					// All instances of .x will be obfuscated
	
	var someOtherObject={};
	someOtherObject.x=24;		// Including this reference to .x
	

To prevent obfuscation, use a string index:

	{{javascript}}
	someOtherObject["x"]=24;	// This reference to member x will never be obfuscated
								// (but will be optimized down to `someOtherObject.x`)
	
See also [Combining Multiple Files](usage-multifile.md) and [Preserved Comments](usage-comments.md).
	
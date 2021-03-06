# MiniME - Open-source JavaScript Minifier

## Sorry :( This Project is not longer under development or maintenance.  The repo is kept here for posterity only.

MiniME is an open-source JavaScript minifier that runs on the .NET platform (Windows or Mono).

If you don't know what a JavaScript minifier is, you probably don't need one but 
there's a good explanation [here](http://en.wikipedia.org/wiki/Minification_(programming)).

## At a Glance ##

* Removes whitespace, comments and redundant braces, parentheses and semicolons.
* Obfuscates local variables and nested functions.
* Detects and removes constant variables.
* Opt-in obfuscation of object properties and methods.
* Opt-in comment preservation.
* Optional code quality warnings (aka "lint") that won't hurt your feelings.
* Can combine multiple scripts into one.
* Can also compress CSS files.
* Command line and .NET API.
* Runs on Windows (.NET 3.5) and under Mono 2.6 (Linux/Mac OS X).
* Tiny <50k download.

## Command Line or API ##

Use it from the command line:

	mm MyJavaScriptFile.js
	
or programatically from .NET code:

```C#
var mm = new MiniME.Compiler();
mm.AddFile("MyJavaScriptFile.js");
var minified=mm.CompileToString();
```

## More Information ##

* [Command Line](Doc/usage.md)
* [API](Doc/usage-api.md)
* [In-script Directives](Doc/usage-directives.md)
* [Combining Multiple Files](Doc/usage-multifile.md)
* [Preserved Comments](Doc/usage-comments.md)
* [Build Integration](Doc/usage-build.md)
* [Code Quality (lint)](Doc/lint.md)
* [CSS Compression](Doc/css.md)
* [Implementation Notes](Doc/implementation.md)
* [Support](Doc/support.md);

## License ##


<a target="_blank" rel="license" href="http://creativecommons.org/licenses/by-nc-sa/3.0/"><img alt="Creative Commons License" style="border-width:0" src="http://i.creativecommons.org/l/by-nc-sa/3.0/88x31.png" /></a><br />MiniME by <a target="_blank" href="http://www.toptensoftware.com/minime" property="cc:attributionName" rel="cc:attributionURL">Topten Software</a> is licensed under a <a target="_blank" rel="license" href="http://creativecommons.org/licenses/by-nc-sa/3.0/">Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License</a>.<br />Permissions beyond the scope of this license may be available at <a target="_blank" href="http://www.toptensoftware.com/contact" rel="cc:morePermissions">http://www.toptensoftware.com/contact</a>.


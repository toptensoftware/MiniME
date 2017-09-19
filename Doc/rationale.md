# MiniME - Rationale

You might be wondering....

>	Why write yet another JavaScript minifier?  What's wrong with Google's Closure
	Compiler, YuiCompressor or any of the other JavaScript minifiers?

Yes, there are other tools and there's nothing particularly wrong with them. I just 
wanted something a little different, something that fitted better with the way I'd use it:

So, here's the main reasons for developing MiniME:

Minimal Runtime Dependencies
:	Just about every project I work on, is developed under Windows, typically using
	Visual Studio.  I wanted something where I could bundle the minifier into the
	project repository and not have to worry about additional dependencies (like 
	Java).  Java is not part of our standard build environment and I didn't want to 
	add it.
	
Better Obfuscation of Constants
:	The JavaScript library I was working on at the time I decided to write MiniME uses
	alot of constant variables.  I wanted to make sure these got optimized away.
	
Simple Opt-in Obfuscation of Members
:	The same JavaScript library mentioned above had alot of internal member symbols, but 
	only one public method.  I wanted a minifier where I could easily declare members that 
	could be obfuscated - on an opt-in basis.
	
A Fun Challenge
:	I've always been interested in language processing - tokenizers, parsers, compilers,
	code optimization and generation etc... but never had a simple enough project to 
	play around in this area.  MiniME proved to be a perfect little project for this
	and in the end it was a fun challenge.  (this is also an excuse to explain away
	the next reason....)

Not Invented Here Syndrome
:   Not at all a good reason - but a real one none the less.


You might also be wondering:

> Where's the name "MiniME" come from?

Isn't it obvious?  Besides the fact that it creates a "Mini-me" of your original JavaScript, 
it's also a **Mini** **M**inifier **E**ngine.
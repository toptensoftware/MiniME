# MiniME - Build Integration

This page gives some tips on how to integrate MiniME into your build procedure.

## Visual Studio ##

The easiest way to integrate MiniME with a Visual Studio project is with a simple
post-build command.

1. Create a text response file that describes the command line options to be used. I usually
	call it `MiniME.txt` and save it in the same folder as the script files.  
	
	Here's a typical example:

		-nologo
		-check-filetimes
		*.js
		-o:scripts.min.js
		
		
	In this example, MiniME will process all `.js` files in the same folder as `MiniME.txt` and
	generate a single output script `scripts.min.js`.  Modify the above example to suit your
	own needs.   
	
	See [Using the Command Line](usage.md) for available options.
		
2. In Solution Explorer, right click on your project and select Properties.

3. Switch to the Build Events tab

4. In the Pre-build event command line, enter something similar to:

		mm.exe @$(ProjectDir)js\MiniME.txt
		
	the @ symbols tells MiniME to read it's command line options from the specified file.
	
5. Make sure mm.exe is on your build environment path (or include the full path to it 
	in the previous step)

6. Build your project and check the `scripts.min.js` file has been generated.


## MSBuild ##

> coming soon

## Make ##

There's no special instructions for integrating MiniME with make base build systems.  

MiniME operates like any typical command line build tool and should be easy to setup for anyone
familiar with configuring make build scripts.

MiniME has an exit code of 0 on success.  Any non-zero exit code indicates failure.
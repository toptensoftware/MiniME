# MiniME - CSS

In additional to minifying JavaScript, MiniME can also compress CSS files.  Usage is pretty much automatic - pass it a file with a `.css` file extension and MiniME will switch into CSS compression mode.

A couple of notes:

* You can't compress CSS and minify JavaScript in the one invocation of MiniME - it's one or the other.

* You can force CSS mode with the `-css` command line switch, or the `MinifyKind` property on the .NET class. 

* Preserved comments work in CSS  minification mode with a leading exclamation point eg:  

        /*! Keep this comment */
        
* MiniME can combine multiple CSS files into one - just pass multiple input files on the command line.

* The default output file name is the same as the first input file with the extension changed to `.min.css`

* The `-linelen:N` command line option can be used to insert line breaks.

* File time checking, output file options and most other non-JavaScript specific command line options still apply - others will be silently ignored.

### Credits

MiniME's CSS compression algorithm is based exactly on the [Isaac Schlueter's CSSMIN](http://github.com/isaacs/cssmin). Thanks Isaac.


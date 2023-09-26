# VS-NoMixedLineEndings
 Visual Studio 2019 or 2022 extension: never save a file with mixed CRLF and LF line endings.  Mixed files are
 turned into fully LF files upon save.  If the file also contains CR-only line endings, it is not
 modified at all.  The choice of turning the file into LF (and not CRLF) is because some Visual Studio
 editing operations introduce CRLFs even in fully LF files, so you can easily get mixed files that way.

 Pre-compiled installation for Visual Studio 2019: double-click on VS2019/NoMixedLineEndings/bin/Release/NoMixedLineEndings.vsix.

 Pre-compiled installation for Visual Studio 2022: double-click on VS2022/bin/Release/VS2022-NoMixedLineEndings.vsix.

 From source, you can also change the constant 'TRIM_END_OF_LINES_TOO' to 'true' and recompile:
 doing so makes the extension also trim whitespace at the end of the lines and ensure the file
 ends in a newline.

 A small issue with this extension (and likely many others) is that it might take some time after
 opening a solution to have the extension loaded.  If you modify and save a file quickly, it might
 be saved when the extension is not loaded yet.

## Coming from...

 This is all MIT-Licensed code.  Parts of it are copy-pasted from the old extension
 https://github.com/idct-tech/vs-trim-line-ends-on-save-plugin
 that I think only works on VS up to 2015.  Thanks to idct-tech!

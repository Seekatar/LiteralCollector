# LiteralCollector
C# Roslyn command line app that gets all the literals and constants from a source tree and puts them in a database

This is a little VisualStudio 2015 console application that gets all the cs files in a folder and uses Rosyln to parse them and extracts out all the string and numeric literals and constants putting them in a database.

It was written to aid in refactoring code that had a huge number of magic strings and numbers.

This uses several C# 6 features just for fun
  * Using static
  * String interpolation
  * Null conditional operator
  * Auto-property initializer
  * Conditional Exceptions
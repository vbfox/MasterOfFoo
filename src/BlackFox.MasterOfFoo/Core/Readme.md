# Core

Core is the part of MasterOfFoo that is extracted from the F# Compiler.

# Extraction Process

* Copy `printf.fs` from the compiler and fix namespace
* Copy `FormatOptions` into `sformat.fs` and all new functions necessary. Keep `anyToStringForPrintf`.
* Extract `PrintfEnv` into `PrintEnv.fs`
* Move `FormatSpecifier` and `FormatFlags` to `FormatSpecification.fs`
* Replace `PrintfEnv` signature with `abstract Write : PrintableElement -> unit` & fix thigns
* `findNextFormatSpecifier` returns `PrintableElement`
* The cornerstone is that ValueConverter should now return a function generating PrintableElement instances
  instead of strings.

There are quite a few things to fix but the original `printf.fs` and `sformat.fs` are commited to serve as guide by
diffing the current code and new versions before upgrade.

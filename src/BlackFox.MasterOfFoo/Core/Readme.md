# Core

Core is the part of MasterOfFoo that is extracted from the F# Compiler.

# Extraction Process

* Copy `printf.fs` from the compiler and fix namespace
* Copy `FormatOptions` into `sformat.fs` and all new functions necessary. Keep `anyToStringForPrintf`.
* Extract `PrintfEnv` into `PrintEnv.fs`
* Move `FormatSpecifier` and `FormatFlags` to `FormatSpecification.fs`
* Replace `PrintfEnv` signature with `abstract Write : PrintableElement -> unit` & fix thigns
* `findNextFormatSpecifier` returns `PrintableElement`

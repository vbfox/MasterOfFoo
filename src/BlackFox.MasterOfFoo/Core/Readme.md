# Core

Core is the part of MasterOfFoo that is extracted from the F# Compiler.

# Extraction Process

* Copy [`printf.fs`][printf_fs] into `printf.fs` and fix namespace
* Copy a small subset of [`sformat.fs`][sformat_fs] into `sformat.fs`. Currently we only need `FormatOptions` and
  `Display.anyToStringForPrintf`.
* Extract `PrintfEnv` from `printf.fs` into `PrintEnv.fs`
* Move `FormatSpecifier` and `FormatFlags` from `printf.fs` to `FormatSpecification.fs`
* Replace `PrintfEnv` signature with `abstract Write : PrintableElement -> unit` & fix things
* Make `findNextFormatSpecifier` returns `PrintableElement`
* The cornerstone is that ValueConverter should now return a function generating `PrintableElement` instances
  instead of strings.

There are quite a few things to fix but the original `printf.fs` and `sformat.fs` are commited to serve as guide by
diffing the current code and new versions before upgrade.

[printf_fs]: https://github.com/dotnet/fsharp/blob/main/src/FSharp.Core/printf.fs
[sformat_fs]: https://github.com/dotnet/fsharp/blob/main/src/Compiler/Utilities/sformat.fs

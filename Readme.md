MasterOfFoo
===========

[![Join the chat at https://gitter.im/vbfox/MasterOfFoo](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/vbfox/MasterOfFoo?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/qp2g8ufg75ogv0k5/branch/master?svg=true)](https://ci.appveyor.com/project/vbfox/masteroffoo/branch/master)
[![Travis-CI Build status](https://travis-ci.org/vbfox/MasterOfFoo.svg?branch=master)](https://travis-ci.org/vbfox/MasterOfFoo)

A library to allow using `printf` style strings in more places.

The code is essentially an extracted version of [`printf.fs`][printf_fs] where the environement can not only decide
what to do with the final blocks that compose the string (printf put them on the console, sprintf in a buffer, ...)
but also what to do with the parameters passed for each format specifier.

Sample usage
------------

```fsharp
module MyModule =
    open System.Text
    open BlackFox.MasterOfFoo
    type private MySprintfEnv() =
        inherit PrintfEnv<unit, string, string>()
        let buf = StringBuilder()
        override this.Finalize() = buf.ToString ()
        override this.Write(s : PrintableElement) = ignore(buf.Append(s.FormatAsPrintF()))
        override this.WriteT(s : string) = ignore(buf.Append(s))

    let mysprintf (format: Format<'T, unit, string, string>) =
        doPrintfFromEnv format (MySprintfEnv())

MyModule.mysprintf "Hello %s." "World"
```

Mini-Doc
--------

### PrintableElement

`PrintableElement` represent an element in a string, for example `sprintf "Foo %s bar" "x"` produce 3
`PrintableElement`, the first contains the string `"Foo "` the second is a format specifier `'s'` with an associated
string value `"x"` and then there is the string  the string `" Bar"`.

Members :

* `ElementType`: Tell your if this is a string or a format specifier.
* `Value`: give the value if it was a format specifier.
* `ValueType`: give the type of value expected by the format specifier.
* `StarWidth`: The width if specified via another parameter as in "%*i".
* `StarPrecision`: The precision if specified via another parameter as in "%.*f".
* `FormatAsPrintF()`: Get the string representation that printf would have normally generated.
* `Specifier`: The format specification for format specifiers.

### PrintfEnv

`PrintfEnv` is the type to implement to create a printf variant it has 3 type parameters:

* `'State`: The state of the printer, passed as argument when using '%t'.
* `'Residue`: The type that methods passed to '%t' must return.
* `'Result`: The final result type for the printer.

Members:
* `Finalize`: Create the final result for this printer
* `Write`: Write an element from the format string to the printer
* `WriteT`: Write the result of the method provided by %t to the printer.

### Functions

* `doPrintfFromEnv`: Take a format and a `PrintfEnv` to create a printf-like function
* `doPrintf`: Same as `doPrintfFromEnv` but allow to know the number of elements when the `PrintfEnv` is created.

FAQ
---

### What does it allow exactly that can't be done with the original set of functions ?

* Generating complex object that aren't only a string like an `SqlCommand` or structured logging.
* Escaping parts in strings, like an `xmlprintf` that would escape `<` to `&lt` in parameters but not in the format
  string.

### What are the limitations ?

The main limitation is that the F# compiler allow a strict set of things an you can't go differently.
The function signature that is the first argument to `Format<_,_,_,_,>` is generated from rules in the compiler and no
library can change them.

The consequence is that we're limited to what is present in the F# compiler, can't add a `%Z` or allow `%0s` to work.

### Aren't you just replicating `ksprintf` ?

`ksprintf` allow you to run code on the final generated result, essentially allowing you to run code during
`PrintfEnv.Finalize` but you can't manipualte the format specifiers or their parameters.

### What this `Star` syntax

When `*` is specified for either the width or the precision an additional parameter is taken by the format to get the
value.

````
> sprintf "%*.*f";;
val it : (int -> int -> float -> string) = <fun:it@1>
````

More fun ?
----------

```fsharp
module ColorPrintf =
    open System
    open System.Text
    open BlackFox.MasterOfFoo

    type private Colorize<'Result>(k) =
        inherit PrintfEnv<unit, string, 'Result>()
        override this.Finalize() : 'Result = k()
        override this.Write(s : PrintableElement) =
            match s.ElementType with
            | PrintableElementType.FromFormatSpecifier ->
                let color = Console.ForegroundColor
                Console.ForegroundColor <- ConsoleColor.Blue
                Console.Write(s.FormatAsPrintF())
                Console.ForegroundColor <- color
            | _ -> Console.Write(s.FormatAsPrintF())
        override this.WriteT(s : string) =
            let color = Console.ForegroundColor
            Console.ForegroundColor <- ConsoleColor.Red
            Console.Write(s)
            Console.ForegroundColor <- color

    let colorprintf (format: Format<'T, unit, string, unit>) =
        doPrintfFromEnv format (Colorize id)

ColorPrintf.colorprintf "%s est %t" "La vie" (fun _ -> "belle !")
```

[printf_fs]: https://github.com/fsharp/fsharp/blob/master/src/fsharp/FSharp.Core/printf.fs

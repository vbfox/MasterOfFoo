/// String interpolation, which needs an F# 5.0 compiler and FSharp.Core 5.0+
/// (RFC FS-1001). Untyped ".NET-style" holes reach the library as a 'P'
/// specifier carrying InteropHoleDotNetFormat.
module Cases.Interpolation50

open Common

type Formattable() =
    interface System.IFormattable with
        member _.ToString(format: string, _provider: System.IFormatProvider) = "Formattable(" + format + ")"

    override _.ToString() = "Formattable"

let run () =
    section "Interpolation50"

    let str = "Foo"
    let number = 42
    let pi = System.Math.PI

    // No holes
    check "no hole" (mysprintf $"Hello") (sprintf $"Hello")

    // Typed ("printf-style") holes
    check "typed string" (mysprintf $"%s{str}") (sprintf $"%s{str}")
    check "typed int" (mysprintf $"%i{number}") (sprintf $"%i{number}")
    check "typed float precision" (mysprintf $"%0.3f{pi}") (sprintf $"%0.3f{pi}")
    check "typed A" (mysprintf $"%A{number}") (sprintf $"%A{number}")

    // Untyped (".NET-style") holes
    check "untyped string" (mysprintf $"{str}") (sprintf $"{str}")
    check "untyped int" (mysprintf $"{number}") (sprintf $"{number}")
    check "untyped literal" (mysprintf $"""{"embedded"}""") (sprintf $"""{"embedded"}""")

    // Untyped holes carrying a .NET format string
    check "untyped dotnet format" (mysprintf $"{pi:N3}") (sprintf $"{pi:N3}")
    check "untyped IFormattable" (mysprintf $"{Formattable():HelloWorld}") (sprintf $"{Formattable():HelloWorld}")

    check
        "untyped IFormattable spaced"
        (mysprintf $"{Formattable():``Hello World``}")
        (sprintf $"{Formattable():``Hello World``}")

    // Mixed with surrounding text and several holes
    check "mixed holes" (mysprintf $"a{str}b%i{number}c") (sprintf $"a{str}b%i{number}c")

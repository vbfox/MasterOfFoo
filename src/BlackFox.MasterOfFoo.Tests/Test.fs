module BlackFox.MasterOfFoo.DoPrintfTests

open Expecto
open System.Text

open BlackFox.MasterOfFoo

type TestEnv() =
    inherit PrintfEnv<unit, string, string>()
    let buf = StringBuilder()
    override __.Finalize() = buf.ToString ()
    override __.Write(s : PrintableElement) = ignore(buf.Append(s.FormatAsPrintF()))
    override __.WriteT(s : string) = ignore(buf.Append(s))

let testprintf (format: Printf.StringFormat<'a>) = doPrintf format (fun _ -> TestEnv())
let coreprintf = FSharp.Core.Printf.sprintf

type Discriminated = |A of string | B of int

let testStr = "Foo"
let testInt = 42

type MyFormatable() =
    interface System.IFormattable with
        member __.ToString(format: string, _formatProvider: System.IFormatProvider) =
            $"MyFormatable(%s{format})"

    override _.ToString() = "MyFormatable"

let tests = [
    test "simple string" {
        Expect.equal
            (coreprintf "Foo")
            (testprintf "Foo")
            "Foo"
    }

    test "simple string interpolation" {
        Expect.equal
            (coreprintf $"Foo")
            (testprintf $"Foo")
            "Foo"
    }

    test "string format" {
        Expect.equal
            (coreprintf "%s" "Foo")
            (testprintf "%s" "Foo")
            "%s"
    }

    test "string format width" {
        Expect.equal
            (coreprintf "%1s" "Foo")
            (testprintf "%1s" "Foo")
            "%1s"
        Expect.equal
            (coreprintf "%5s" "Foo")
            (testprintf "%5s" "Foo")
            "%5s"
    }

    test "string untyped interpolation" {
        Expect.equal
            (coreprintf $"""{"Foo"}""")
            (testprintf $"""{"Foo"}""")
            "%s"
    }

    test "string typed interpolation" {
        Expect.equal
            (coreprintf $"""%s{"Foo"}""")
            (testprintf $"""%s{"Foo"}""")
            "%s"
    }

    test "int format %i" {
        Expect.equal
            (coreprintf "%i" 5)
            (testprintf "%i" 5)
            "%i"
    }

    test "int format %B" {
        Expect.equal
            (coreprintf "%B" 5)
            (testprintf "%B" 5)
            "%B"
    }

    test "int untyped interpolation" {
        Expect.equal
            (coreprintf $"{5}")
            (testprintf $"{5}")
            "{5}"
    }

    test "int untyped interpolation .NET format" {
        Expect.equal
            (coreprintf $"{System.Math.PI:N3}")
            (testprintf $"{System.Math.PI:N3}")
            "{System.Math.PI:N3}"
    }

    test "int typed interpolation" {
        Expect.equal
            (coreprintf $"%i{5}")
            (testprintf $"%i{5}")
            "%i"
    }

    test "int format width" {
        Expect.equal
            (coreprintf "%1i" 5)
            (testprintf "%1i" 5)
            "%1i"
        Expect.equal
            (coreprintf "%5i" 5)
            (testprintf "%5i" 5)
            "%5i"
    }

    test "A format" {
        Expect.equal
            (coreprintf "%A %A %A %A %A" "Foo" 5 (A("Foo")) (B(42)) System.ConsoleColor.Red)
            (testprintf "%A %A %A %A %A" "Foo" 5 (A("Foo")) (B(42)) System.ConsoleColor.Red)
            "%A %A %A %A %A"
    }

    test "custom untyped interpolation" {
        Expect.equal
            (coreprintf $"{MyFormatable()}")
            (testprintf $"{MyFormatable()}")
            "{MyFormatable()}"
    }

    test "custom untyped interpolation .NET format" {
        Expect.equal
            (coreprintf $"{MyFormatable():HelloWorld}")
            (testprintf $"{MyFormatable():HelloWorld}")
            "{MyFormatable():HelloWorld}"
    }

    test "custom untyped interpolation .NET format unusual characters" {
        // Using double backticks to escape the format string if it contains spaces for example
        Expect.equal
            (coreprintf $"{MyFormatable():``Hello World``}")
            (testprintf $"{MyFormatable():``Hello World``}")
            "{MyFormatable():``Hello World``}"
    }
]

[<Tests>]
let test = testList "Tests" tests

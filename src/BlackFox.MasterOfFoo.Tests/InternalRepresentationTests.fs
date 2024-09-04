module BlackFox.MasterOfFoo.Tests.InternalRepresentationTests

open Expecto
open System.Text

open BlackFox.MasterOfFoo
open System.Text.RegularExpressions

type MyToStringSaysHi() =
    override _.ToString() =
        "Hi"

type TestEnv() =
    inherit PrintfEnv<unit, string, string>()
    let buf = StringBuilder().AppendLine("Init")
    override _.Finalize() =
        buf.AppendLine("Finalize") |> ignore
        buf.ToString ()
    override _.Write(s : PrintableElement) =
        buf.Append("Write ") |> ignore
        buf.Append(sprintf "%A" s) |> ignore
        buf.AppendLine(";") |> ignore
    override _.WriteT(s : string) =
        buf.Append("WriteT ") |> ignore
        buf.AppendLine(s) |> ignore

let cleanTypeParameters (s: string) =
    let re = Regex("""\[\[([^,]+)([^\]]*)\]\]""")
    let replacement (m: Match) =
        sprintf "[[%s]]" m.Groups.[1].Value
    re.Replace(s, replacement)

let testprintf (format: Printf.StringFormat<'a>) =
    doPrintf format (fun _ -> TestEnv())

let cleanText (s: string) =
    s.Trim().Replace("\r\n", "\n")

let testEqual (actual: string) (expected:string) =
    Expect.equal (actual |> cleanText |> cleanTypeParameters) (expected |> cleanText) ""

let tests = [
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Simple case

    test "simple string" {
        testEqual
            (testprintf "Foo")
            """
Init
Write value: "Foo", type: Direct, valueType: System.String, spec: , starWidth: , starPrecision: , AsPrintF: Foo;
Finalize
"""
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Supported types

    test "string hole only" {
        testEqual
            (testprintf "%s" "Foo")
            """
Init
Write value: "Foo", type: FromFormatSpecifier, valueType: System.String, spec: 's', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: Foo;
Finalize
"""
    }

    test "boolean hole only" {
        testEqual
            (testprintf "%b" true)
            """
Init
Write value: true, type: FromFormatSpecifier, valueType: System.Boolean, spec: 'b', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: true;
Finalize
"""
    }

    test "int hole only %i" {
        testEqual
            (testprintf "%i" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'i', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42;
Finalize
"""
    }

    test "int hole only %d" {
        testEqual
            (testprintf "%d" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'd', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42;
Finalize
"""
    }

    test "int hole only %u" {
        testEqual
            (testprintf "%u" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'u', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42;
Finalize
"""
    }

    test "int hole only %x" {
        testEqual
            (testprintf "%x" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'x', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 2a;
Finalize
"""
    }

    test "int hole only %X" {
        testEqual
            (testprintf "%X" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'X', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 2A;
Finalize
"""
    }

    test "int hole only %o" {
        testEqual
            (testprintf "%o" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'o', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 52;
Finalize
"""
    }

    test "float64 hole only %f" {
        testEqual
            (testprintf "%f" 42.42)
            """
Init
Write value: 42.42, type: FromFormatSpecifier, valueType: System.Double, spec: 'f', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42.420000;
Finalize
"""
    }

    test "float64 hole only %e" {
        testEqual
            (testprintf "%e" 42.42)
            """
Init
Write value: 42.42, type: FromFormatSpecifier, valueType: System.Double, spec: 'e', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 4.242000e+001;
Finalize
"""
    }

    test "float64 hole only %E" {
        testEqual
            (testprintf "%E" 42.42)
            """
Init
Write value: 42.42, type: FromFormatSpecifier, valueType: System.Double, spec: 'E', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 4.242000E+001;
Finalize
"""
    }

    test "float64 hole only %g" {
        testEqual
            (testprintf "%g" 42.42)
            """
Init
Write value: 42.42, type: FromFormatSpecifier, valueType: System.Double, spec: 'g', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42.42;
Finalize
"""
    }

    test "float64 hole only %G" {
        testEqual
            (testprintf "%G" 42.42)
            """
Init
Write value: 42.42, type: FromFormatSpecifier, valueType: System.Double, spec: 'G', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42.42;
Finalize
"""
    }

    test "float32 hole only %f" {
        testEqual
            (testprintf "%f" 42.0f)
            """
Init
Write value: 42.0f, type: FromFormatSpecifier, valueType: System.Single, spec: 'f', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42.000000;
Finalize
"""
    }

    test "Decimal hole only" {
        testEqual
            (testprintf "%M" 123456789.123456789M)
            """
Init
Write value: 123456789.123456789M, type: FromFormatSpecifier, valueType: System.Decimal, spec: 'M', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 123456789.123456789;
Finalize
"""
    }

    test "F# format hole only" {
        testEqual
            (testprintf "%A" (Some true))
            """
Init
Write value: Some true, type: FromFormatSpecifier, valueType: Microsoft.FSharp.Core.FSharpOption`1[[System.Boolean]], spec: 'A', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: Some true;
Finalize
"""
    }

    test "ToString hole only" {
        testEqual
            (testprintf "%O" (MyToStringSaysHi()))
            """
Init
Write value: Hi, type: FromFormatSpecifier, valueType: BlackFox.MasterOfFoo.Tests.InternalRepresentationTests+MyToStringSaysHi, spec: 'O', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: Hi;
Finalize
"""
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Complex case mixing holes and text

    test "hole and text" {
        testEqual
            (testprintf "Hello%sWorld" "Foo")
            """
Init
Write value: "Hello", type: Direct, valueType: System.String, spec: , starWidth: , starPrecision: , AsPrintF: Hello;
Write value: "Foo", type: FromFormatSpecifier, valueType: System.String, spec: 's', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: Foo;
Write value: "World", type: Direct, valueType: System.String, spec: , starWidth: , starPrecision: , AsPrintF: World;
Finalize
"""
    }

    test "multiple holes and text" {
        testEqual
            (testprintf "Hello%sWorld%i" "Foo" 42)
            """
Init
Write value: "Hello", type: Direct, valueType: System.String, spec: , starWidth: , starPrecision: , AsPrintF: Hello;
Write value: "Foo", type: FromFormatSpecifier, valueType: System.String, spec: 's', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: Foo;
Write value: "World", type: Direct, valueType: System.String, spec: , starWidth: , starPrecision: , AsPrintF: World;
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'i', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42;
Finalize
"""
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Integer options

    test "plus sign" {
        testEqual
            (testprintf "%+i" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'i', Precision=-, Width=-, Flags=PlusForPositives, starWidth: , starPrecision: , AsPrintF: +42;
Finalize
"""
    }

    test "blank plus sign" {
        testEqual
            (testprintf "% i" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'i', Precision=-, Width=-, Flags=SpaceForPositives, starWidth: , starPrecision: , AsPrintF:  42;
Finalize
"""
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Alignment

    test "string alignment" {
        testEqual
            (testprintf "%5s" "Foo")
            """
Init
Write value: "Foo", type: FromFormatSpecifier, valueType: System.String, spec: 's', Precision=-, Width=5, Flags=None, starWidth: , starPrecision: , AsPrintF:   Foo;
Finalize
"""
    }

    test "int alignment" {
        testEqual
            (testprintf "%5i" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'i', Precision=-, Width=5, Flags=None, starWidth: , starPrecision: , AsPrintF:    42;
Finalize
"""
    }

    test "int pad with 0" {
        testEqual
            (testprintf "%05i" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'i', Precision=-, Width=5, Flags=PadWithZeros, starWidth: , starPrecision: , AsPrintF: 00042;
Finalize
"""
    }

    test "string left alignment" {
        testEqual
            (testprintf "%-5s" "Foo")
            """
Init
Write value: "Foo", type: FromFormatSpecifier, valueType: System.String, spec: 's', Precision=-, Width=5, Flags=LeftJustify, starWidth: , starPrecision: , AsPrintF: Foo  ;
Finalize
"""
    }

    test "int left alignment" {
        testEqual
            (testprintf "%-5i" 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'i', Precision=-, Width=5, Flags=LeftJustify, starWidth: , starPrecision: , AsPrintF: 42   ;
Finalize
"""
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Star alignment

    test "string star alignment" {
        testEqual
            (testprintf "%*s" 5 "Foo")
            """
Init
Write value: "Foo", type: FromFormatSpecifier, valueType: System.String, spec: 's', Precision=-, Width=*, Flags=None, starWidth: 5, starPrecision: , AsPrintF:   Foo;
Finalize
"""
    }

    test "int star alignment" {
        testEqual
            (testprintf "%*i" 5 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'i', Precision=-, Width=*, Flags=None, starWidth: 5, starPrecision: , AsPrintF:    42;
Finalize
"""
    }

    test "string left star alignment" {
        testEqual
            (testprintf "%-*s" 5 "Foo")
            """
Init
Write value: "Foo", type: FromFormatSpecifier, valueType: System.String, spec: 's', Precision=-, Width=*, Flags=LeftJustify, starWidth: 5, starPrecision: , AsPrintF: Foo  ;
Finalize
"""
    }

    test "int left star alignment" {
        testEqual
            (testprintf "%-*i" 5 42)
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Int32, spec: 'i', Precision=-, Width=*, Flags=LeftJustify, starWidth: 5, starPrecision: , AsPrintF: 42   ;
Finalize
"""
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Interpolation

    test "interpolated-explicit string hole only" {
        let value = "Foo"
        testEqual
            (testprintf $"%s{value}")
            """
Init
Write value: "Foo", type: FromFormatSpecifier, valueType: System.Object, spec: 's', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: Foo;
Finalize
"""
    }

    test "interpolated-implicit string hole only" {
        let value = "Foo"
        testEqual
            (testprintf $"{value}")
            """
Init
Write value: "Foo", type: FromFormatSpecifier, valueType: System.Object, spec: 'P', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: Foo;
Finalize
"""
    }

    test "interpolated-embeded string hole only" {
        testEqual
            (testprintf $"""{"embedded string literal"}""")
            """
Init
Write value: "embedded string literal", type: FromFormatSpecifier, valueType: System.Object, spec: 'P', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: embedded string literal;
Finalize
"""
    }

    test "interpolated-explicit int hole only" {
        let value = 42
        testEqual
            (testprintf $"%i{value}")
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Object, spec: 'i', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42;
Finalize
"""
    }

    test "interpolated-implicit int hole only" {
        let value = 42
        testEqual
            (testprintf $"{value}")
            """
Init
Write value: 42, type: FromFormatSpecifier, valueType: System.Object, spec: 'P', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 42;
Finalize
"""
    }

    test "interpolated-explicit float hole F# format only" {
        testEqual
            (testprintf $"%0.3f{System.Math.PI}")
            """
Init
Write value: 3.141592654, type: FromFormatSpecifier, valueType: System.Object, spec: 'f', Precision=3, Width=-, Flags=PadWithZeros, starWidth: , starPrecision: , AsPrintF: 3.142;
Finalize
"""
    }

    test "interpolated-implicit float hole dotnet format only" {
        testEqual
            (testprintf $"{System.Math.PI:N3}")
            """
Init
Write value: 3.141592654, type: FromFormatSpecifier, valueType: System.Object, spec: 'P', Precision=-, Width=-, Flags=None, starWidth: , starPrecision: , AsPrintF: 3.142;
Finalize
"""
    }
]

[<Tests>]
let test = testList "Internal Representation Tests" tests

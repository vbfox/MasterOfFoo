module BlackFox.MasterOfFoo.DoPrintfTests

open System.Text
open NUnit.Framework
open BlackFox.MasterOfFoo

type TestEnv() = 
    inherit PrintfEnv<unit, string, string>()
    let buf = StringBuilder()
    override __.Finalize() = buf.ToString ()
    override __.Write(s : PrintableElement) = ignore(buf.Append(s.FormatAsPrintF()))
    override __.WriteT(s : string) = ignore(buf.Append(s))

let testprintf (format: Printf.StringFormat<'a>) = doPrintf format (fun _ -> TestEnv())
let coreprintf = FSharp.Core.Printf.sprintf

[<Test>]
let SimpleString () =
    Assert.AreEqual(
        coreprintf "Foo",
        testprintf "Foo")

[<Test>]
let StringFormat () =
    Assert.AreEqual(
        coreprintf "%s" "Foo",
        testprintf "%s" "Foo")

[<Test>]
let StringFormatWidth () =
    Assert.AreEqual(
        coreprintf "%1s" "Foo",
        testprintf "%1s" "Foo")
    Assert.AreEqual(
        coreprintf "%5s" "Foo",
        testprintf "%5s" "Foo")

[<Test>]
let IntFormat () =
    Assert.AreEqual(
        coreprintf "%i" 5,
        testprintf "%i" 5)

[<Test>]
let IntFormatWidth () =
    Assert.AreEqual(
        coreprintf "%1i" 5,
        testprintf "%1i" 5)
    Assert.AreEqual(
        coreprintf "%5i" 5,
        testprintf "%5i" 5)

type Discriminated = |A of string | B of int

[<Test>]
let AFormat () =
    Assert.AreEqual(
        coreprintf "%A %A %A %A %A" "Foo" 5 (A("Foo")) (B(42)) System.ConsoleColor.Red,
        testprintf "%A %A %A %A %A" "Foo" 5 (A("Foo")) (B(42)) System.ConsoleColor.Red)
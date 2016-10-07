module BlackFox.MasterOfFoo.DoPrintfTests

open System
open System.Text
open NUnit.Framework
open BlackFox.MasterOfFoo

type TestEnv() = 
    inherit PrintfEnv<unit, string, string>()
    let buf = StringBuilder()
    override this.Finalize() = buf.ToString ()
    override this.Write(s : PrintableElement) = ignore(buf.Append(s.FormatAsPrintF()))
    override this.WriteT(s : string) = ignore(buf.Append(s))

let testprintf (format: Format<'T, unit, string, string>) =
    doPrintf format (fun _ -> TestEnv())

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
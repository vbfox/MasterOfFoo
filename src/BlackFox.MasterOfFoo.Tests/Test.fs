module BlackFox.MasterOfFoo.DoPrintfTests

open System
open System.Text
open NUnit.Framework
open MasterOfFoo
open MasterOfFoo.Printf

type TestEnv() = 
    inherit PrintfEnv<unit, string, string>()
    let buf = StringBuilder()
    override this.Finalize() = buf.ToString ()
    override this.Write(s : PrintableElement) = ignore(buf.Append(s.ToString()))
    override this.WriteT(s : string) = ignore(buf.Append(s))

let testprintf (format: Format<'T, unit, string, string>) =
    doPrintf format (fun _ -> TestEnv() :> PrintfEnv<_, _, _>)

let coreprintf = FSharp.Core.Printf.sprintf

[<Test>]
let SimpleString () =
    Assert.AreEqual(
        testprintf "Foo",
        coreprintf "Foo")


[<Test>]
let StringFormat () =
    Assert.AreEqual(
        testprintf "%s" "Foo",
        coreprintf "%s" "Foo")
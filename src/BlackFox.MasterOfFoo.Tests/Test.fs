module BlackFox.MasterOfFoo.DoPrintfTests

open System
open NUnit.Framework
open MasterOfFoo.Printf

type TestEnv<'Result>(k, buf) = 
    inherit PrintfEnv<Text.StringBuilder, unit, 'Result>(buf)
    override this.Finalize() : 'Result = k ()
    override this.Write(s : PrintableElement) = ignore(buf.Append(s.ToString()))
    override this.WriteT(()) = ()

let testprintf (format : StringFormat<'T>)  =
    doPrintF

[<Test>]
let TestCase () =
    Assert.IsTrue(true)


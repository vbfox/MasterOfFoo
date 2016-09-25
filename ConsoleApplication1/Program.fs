// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open MasterOfFoo.Core

type internal MyEnv<'Result>(k, state) = 
    inherit PrintfEnv<unit, unit, 'Result>(state)
    override this.Finalize() : 'Result =
        printfn "Finalizing"
        k ()
    override this.Write(s : string) =
        printfn "Writing: '%s'" s
    override this.WriteT(()) = 
        printfn "WTF"

type MyFormat<'T, 'Result>  = Format<'T, unit, unit, 'Result>
type MyFormat<'T>  = MyFormat<'T, unit>

let testprintf (format : MyFormat<'T>) =
    PrintfImpl.doPrintf format (fun n -> 
        MyEnv(ignore, ()) :> PrintfEnv<_, _, _>
    )


[<EntryPoint>]
let main argv = 
    testprintf "Hello %s %s %s %s %s %s %06i" "1" "2" "3" "4" "5" "6" 5
    ignore(System.Console.ReadLine ())
    0 // return an integer exit code

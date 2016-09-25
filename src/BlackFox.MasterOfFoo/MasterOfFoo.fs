namespace MasterOfFoo

open MasterOfFoo.Core

module Sampling =

    type internal MyEnv<'Result>(k, state) = 
        inherit PrintfImpl.PrintfEnv<unit, unit, 'Result>(state)
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
            MyEnv(ignore, ()) :> PrintfImpl.PrintfEnv<_, _, _>
        )

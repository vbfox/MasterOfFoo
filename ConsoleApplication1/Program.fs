// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System.Text
open MasterOfFoo

type internal MyEnv<'Result>(k, state) = 
    inherit PrintfEnv<StringBuilder, unit, 'Result>(state)
    override this.Finalize() : 'Result =
        printfn "Finalizing"
        k ()
    override this.Write(s : PrintableElement) =
        printfn "Writing: %A" s
        state.Append(s.FormatAsPrintF()) |> ignore
    override this.WriteT(()) = 
        printfn "WTF"

type MyFormat<'T, 'Result>  = Format<'T, StringBuilder, unit, 'Result>
type MyFormat<'T>  = MyFormat<'T, unit>

let testprintf (sb: StringBuilder) (format : MyFormat<'T>) =
    Printf.doPrintf format (fun n -> 
        MyEnv(ignore, sb) :> PrintfEnv<_, _, _>
    )

let simple () =
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello %-010i hello %s" 1000 "World"
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

let percentStar () =
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello '%*i'" 5 42
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

let chained () =
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello %s %s %s %s %s %s" "1" "2" "3" "4" "5" "6"
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

let complex () = 
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello %s %s %s %s %s %s %06i %t" "1" "2" "3" "4" "5" "6" 5 (fun x -> x.Append("CALLED") |> ignore)
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

[<EntryPoint>]
let main argv = 
    simple ()
    percentStar ()
    //chained ()
    //complex ()
    ignore(System.Console.ReadLine ())
    0 // return an integer exit code

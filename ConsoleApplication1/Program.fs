// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open MasterOfFoo.Sampling

[<EntryPoint>]
let main argv = 
    testprintf "Hello %s %s %s %s %s %s %06i" "1" "2" "3" "4" "5" "6" 5
    ignore(System.Console.ReadLine ())
    0 // return an integer exit code

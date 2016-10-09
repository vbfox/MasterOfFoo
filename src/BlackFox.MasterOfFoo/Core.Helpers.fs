[<AutoOpen>]
module internal BlackFox.MasterOfFoo.Core.Helpers

open System
open System.Reflection

let inline (===) a b = Object.ReferenceEquals(a, b)

let nonPublicStatics = BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Static

let inline verifyMethodInfoWasTaken (_mi : System.Reflection.MemberInfo) =
#if DEBUG
    if isNull _mi then 
        ignore (System.Diagnostics.Debugger.Break())
#else
    ()
#endif
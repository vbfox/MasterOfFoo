[<AutoOpen>]
module internal MasterOfFoo.Core.Helpers

open System
open System.Reflection

let inline (===) a b = Object.ReferenceEquals(a, b)

let NonPublicStatics = BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Static
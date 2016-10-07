[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<AutoOpen>]
module BlackFox.MasterOfFoo.MasterOfFoo 

open System
open BlackFox.MasterOfFoo.Core

let doPrintf (fmt: Format<'Printer, 'State, 'Residue, 'Result>) (f: int -> #PrintfEnv<'State, 'Residue, 'Result>) = 
    let formatter, n = PrintfCache.Cache<_, _, _, _>.Get fmt
    let env() = f(n) :> PrintfEnv<_,_,_>
    formatter env

let doPrintfFromEnv (fmt: Format<'Printer, 'State, 'Residue, 'Result>) (env: #PrintfEnv<_,_,_>) =
    let formatter, n = PrintfCache.Cache<_, _, _, _>.Get fmt
    formatter (fun _ -> env :> PrintfEnv<_,_,_>)
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<AutoOpen>]
module BlackFox.MasterOfFoo.MasterOfFoo 

open System
open BlackFox.MasterOfFoo.Core

let doPrintf (format: Format<'Printer, 'State, 'Residue, 'Result>) (getEnv: int -> #PrintfEnv<'State, 'Residue, 'Result>) = 
    let formatter, n = PrintfCache.Cache<_, _, _, _>.Get format
    let env() = getEnv(n) :> PrintfEnv<_,_,_>
    formatter env

let doPrintfFromEnv (format: Format<'Printer, 'State, 'Residue, 'Result>) (env: #PrintfEnv<_,_,_>) =
    let formatter, n = PrintfCache.Cache<_, _, _, _>.Get format
    formatter (fun _ -> env :> PrintfEnv<_,_,_>)
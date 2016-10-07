[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<AutoOpen>]
module BlackFox.MasterOfFoo.MasterOfFoo 

open System
open BlackFox.MasterOfFoo.Core

/// Take a format and a PrintfEnv builder to create a printf-like function
let doPrintf (format: Format<'Printer, 'State, 'Residue, 'Result>) (getEnv: int -> #PrintfEnv<'State, 'Residue, 'Result>) = 
    let formatter, n = PrintfCache.Cache<_, _, _, _>.Get format
    let env() = getEnv(n) :> PrintfEnv<_,_,_>
    formatter env

/// Take a format and a PrintfEnv to create a printf-like function
let doPrintfFromEnv (format: Format<'Printer, 'State, 'Residue, 'Result>) (env: #PrintfEnv<_,_,_>) =
    let formatter, n = PrintfCache.Cache<_, _, _, _>.Get format
    formatter (fun _ -> env :> PrintfEnv<_,_,_>)
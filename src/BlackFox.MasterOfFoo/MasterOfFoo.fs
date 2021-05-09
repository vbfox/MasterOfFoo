[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<AutoOpen>]
module BlackFox.MasterOfFoo.MasterOfFoo

open BlackFox.MasterOfFoo.Core

/// Take a format and a PrintfEnv builder to create a printf-like function
let doPrintf (format: Format<'Printer, 'State, 'Residue, 'Result>) (envf: int -> #PrintfEnv<'State, 'Residue, 'Result>) =
    let cacheItem = Cache<_, _, _, _>.GetParser format

    match format.Captures with
    | null ->
        // The ksprintf "...%d ...." arg path, producing a function
        let factory = cacheItem.GetCurriedPrinterFactory()
        let initial() = (envf cacheItem.BlockCount :> PrintfEnv<_,_,_>)
        factory.Invoke([], initial)
    | captures ->
        // The ksprintf $"...%d{3}...." path, running the steps straight away to produce a string
        let steps = cacheItem.GetStepsForCapturedFormat()
        let env = envf cacheItem.BlockCount :> PrintfEnv<_,_,_>
        let res = env.RunSteps(captures, format.CaptureTypes, steps)
        unbox res // prove 'T = 'Result
        //continuation res

/// Take a format and a PrintfEnv to create a printf-like function
let doPrintfFromEnv (format: Format<'Printer, 'State, 'Residue, 'Result>) (env: #PrintfEnv<_,_,_>) =
    doPrintf format (fun _ -> env)

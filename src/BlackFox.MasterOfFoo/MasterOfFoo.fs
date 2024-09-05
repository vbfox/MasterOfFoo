[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<AutoOpen>]
module BlackFox.MasterOfFoo.MasterOfFoo

open System
open System.Reflection
open BlackFox.MasterOfFoo.Core
open System.Collections.Concurrent

module private FormatReflection =
    let capturesCache = ConcurrentDictionary<Type, PropertyInfo>()

    let inline getCaptures (format: Format<'Printer, 'State, 'Residue, 'Result>): obj[] =
        let genericType = format.GetType()
        let prop = capturesCache.GetOrAdd(genericType, fun t -> t.GetProperty("Captures"))
        if prop = null then
            null
        else
            prop.GetValue(format) :?> obj[]

    let captureTypesCache = ConcurrentDictionary<Type, PropertyInfo>()

    let inline getCaptureTypes (format: Format<'Printer, 'State, 'Residue, 'Result>): System.Type[] =
        let genericType = format.GetType()
        let prop = captureTypesCache.GetOrAdd(genericType, fun t -> t.GetProperty("CaptureTypes"))
        if prop = null then
            null
        else
            prop.GetValue(format) :?> Type[]

open FormatReflection

/// Take a format and a PrintfEnv builder to create a printf-like function
let doPrintf (format: Format<'Printer, 'State, 'Residue, 'Result>) (envf: int -> #PrintfEnv<'State, 'Residue, 'Result>) =
    let cacheItem = Cache<_, _, _, _>.GetParser format

    match getCaptures format with
    | null ->
        // The ksprintf "...%d ...." arg path, producing a function
        let factory = cacheItem.GetCurriedPrinterFactory()
        let initial() = (envf cacheItem.BlockCount :> PrintfEnv<_,_,_>)
        factory.Invoke([], initial)
    | captures ->
        // The ksprintf $"...%d{3}...." path, running the steps straight away to produce a string
        let steps = cacheItem.GetStepsForCapturedFormat()
        let env = envf cacheItem.BlockCount :> PrintfEnv<_,_,_>
        let res = env.RunSteps(captures, getCaptureTypes format, steps)
        unbox res // prove 'T = 'Result
        //continuation res

/// Take a format and a PrintfEnv to create a printf-like function
let doPrintfFromEnv (format: Format<'Printer, 'State, 'Residue, 'Result>) (env: #PrintfEnv<_,_,_>) =
    doPrintf format (fun _ -> env)

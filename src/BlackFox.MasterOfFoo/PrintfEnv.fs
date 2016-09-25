namespace MasterOfFoo.Core

open MasterOfFoo.Core.FormatSpecification
open System

type PrintableElementType =
    | MadeByEngine = 0
    | Direct = 1
    | FromFormatSpecifier = 2

[<Struct>]
type PrintableElement(s: string, value: obj, type': PrintableElementType, valueType: Type, spec: FormatSpecifier option) =
    /// Get the string representation that printf would have normally generated
    override x.ToString () = 
        sprintf "value: %A, type: %A, valueType: %s, spec: %s, AsPrintF: %s" value type' (valueType.FullName) (match spec with Some x -> string x.TypeChar |None -> "") s

    member x.FormatAsPrintF() = s
    static member MadeByEngine(s: string) =
        PrintableElement(s, s, PrintableElementType.MadeByEngine, typeof<string>, None)
    static member MakeDirect(s: string) =
        PrintableElement(s, s, PrintableElementType.Direct, typeof<string>, None)
    static member MakeFromFormatSpecifier(s: string, value: obj, valueType: Type, spec: FormatSpecifier) =
        PrintableElement(s, s, PrintableElementType.FromFormatSpecifier, valueType, Some(spec))

/// Abstracts generated printer from the details of particular environment: how to write text, how to produce results etc...
[<AbstractClass>]
type PrintfEnv<'State, 'Residue, 'Result> =
    val State : 'State
    new(s : 'State) = { State = s }
    abstract Finalize : unit -> 'Result
    abstract Write : PrintableElement -> unit
    abstract WriteT : 'Residue -> unit
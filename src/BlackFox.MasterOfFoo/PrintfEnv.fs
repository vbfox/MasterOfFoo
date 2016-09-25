namespace MasterOfFoo.Core

open MasterOfFoo.Core.FormatSpecification
open System

type PrintableElementType =
    | MadeByEngine = 0
    | Direct = 1
    | FromFormatSpecifier = 2

[<Struct>]
type PrintableElement(printer: unit -> string, value: obj, type': PrintableElementType, valueType: Type, spec: FormatSpecifier option) =
    override x.ToString () = 
        sprintf
            "value: %A, type: %A, valueType: %s, spec: %s, AsPrintF: %s"
            value
            type'
            (valueType.FullName)
            (match spec with Some x -> string x.TypeChar | None -> "")
            (x.FormatAsPrintF())

    /// Get the string representation that printf would have normally generated
    member x.FormatAsPrintF() =
        printer ()

    static member inline MakeFromString(s: string, type': PrintableElementType) =
        let printer = fun () -> s
        PrintableElement(printer, s, type', typeof<string>, None)

    static member inline MadeByEngine(s: string) =
        PrintableElement.MakeFromString(s , PrintableElementType.MadeByEngine)

    static member MakeDirect(s: string) =
        PrintableElement.MakeFromString(s , PrintableElementType.Direct)

    static member MakeFromFormatSpecifier(printer: unit -> string, value: obj, valueType: Type, spec: FormatSpecifier) =
        PrintableElement(printer, value, PrintableElementType.FromFormatSpecifier, valueType, Some(spec))

/// Abstracts generated printer from the details of particular environment: how to write text, how to produce results etc...
[<AbstractClass>]
type PrintfEnv<'State, 'Residue, 'Result> =
    val State : 'State
    new(s : 'State) = { State = s }
    abstract Finalize : unit -> 'Result
    abstract Write : PrintableElement -> unit
    abstract WriteT : 'Residue -> unit
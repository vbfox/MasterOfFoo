namespace MasterOfFoo.Core

type PrintableElementType =
    | MadeByEngine = 0
    | DirectFromFormatString = 1
    | TempGenerated = 2

[<Struct>]
type PrintableElement(s: string, value: obj, type': PrintableElementType) =
    /// Get the string representation that printf would have normally generated
    override x.ToString () = 
        sprintf "AsPrintF: %s, value: %A, type: %A" s value type'

    member x.FormatAsPrintF() = s
    static member MadeByEngine(s: string) = PrintableElement(s, s, PrintableElementType.MadeByEngine)
    static member DirectFromFormatString(s: string) = PrintableElement(s, s, PrintableElementType.DirectFromFormatString)
    static member TempGenerated(s: string) = PrintableElement(s, s, PrintableElementType.TempGenerated)

/// Abstracts generated printer from the details of particular environment: how to write text, how to produce results etc...
[<AbstractClass>]
type PrintfEnv<'State, 'Residue, 'Result> =
    val State : 'State
    new(s : 'State) = { State = s }
    abstract Finalize : unit -> 'Result
    abstract Write : PrintableElement -> unit
    abstract WriteT : 'Residue -> unit
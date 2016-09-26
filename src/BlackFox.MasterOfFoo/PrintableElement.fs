namespace MasterOfFoo

open System
open FormatSpecifierConstants

type PrintableElementType =
    | MadeByEngine = 0uy
    | Direct = 1uy
    | FromFormatSpecifier = 2uy

type PrintableElement
    (
        printer: unit -> string,
        value: obj,
        type': PrintableElementType,
        valueType: Type,
        spec: FormatSpecifier,
        starWidth: int,
        starPrecision: int
    ) =

    /// The type of element (From a format specifier or directly from the string string)
    member x.Type with get() = type'
    
    /// The value passed as parameter, of type ValueType
    member x.Value with get() = value
    
    /// The type of the value
    member x.ValueType with get() = valueType
    
    /// The format specification for format specifiers
    member x.Spec with get() = if Object.ReferenceEquals(spec, null) then None else Some(spec)
    
    /// The width if specified via another parameter as in "%*i"
    member x.StarWidth with get() = match starWidth with NotSpecifiedValue -> None | x -> Some(x)
    
    /// The precision if specified via another parameter as in "%.*f"
    member x.StarPrecision with get() = match starPrecision with NotSpecifiedValue -> None | x -> Some(x)

    override x.ToString () = 
        sprintf
            "value: %A, type: %A, valueType: %s, spec: %s, starWidth: %s, starPrecision: %s, AsPrintF: %s"
            value
            type'
            (valueType.FullName)
            (match x.Spec with Some x -> x.ToString() | None -> "")
            (match x.StarWidth with Some x -> x.ToString() | None -> "")
            (match x.StarPrecision with Some x -> x.ToString() | None -> "")
            (x.FormatAsPrintF())

    /// Get the string representation that printf would have normally generated
    member x.FormatAsPrintF() =
        printer ()

    static member inline MakeFromString(s: string, type': PrintableElementType) =
        let printer = fun () -> s
        PrintableElement(printer, s, type', typeof<string>, Unchecked.defaultof<FormatSpecifier>, NotSpecifiedValue, NotSpecifiedValue)

    static member inline MadeByEngine(s: string) =
        PrintableElement.MakeFromString(s , PrintableElementType.MadeByEngine)

    static member MakeDirect(s: string) =
        PrintableElement.MakeFromString(s , PrintableElementType.Direct)

    static member MakeFromFormatSpecifier(printer: unit -> string, value: obj, valueType: Type, spec: FormatSpecifier, starWidth: int, starPrecision: int) =
        PrintableElement(printer, value, PrintableElementType.FromFormatSpecifier, valueType, spec, starWidth, starPrecision)
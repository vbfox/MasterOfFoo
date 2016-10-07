namespace BlackFox.MasterOfFoo

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

    new(s: string, type': PrintableElementType) =
        PrintableElement(
            Unchecked.defaultof<unit -> string>,
            s,
            type',
            Unchecked.defaultof<Type>,
            Unchecked.defaultof<FormatSpecifier>,
            NotSpecifiedValue,
            NotSpecifiedValue)

    /// The type of element (From a format specifier or directly from the string string)
    member x.Type with get() = type'
    
    /// The value passed as parameter, of type ValueType
    member x.Value with get() = value
    
    /// The type of the value
    member x.ValueType
        with get() =
            match type' with
            | PrintableElementType.FromFormatSpecifier -> valueType
            | _ -> typeof<string>    
    
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
            (x.ValueType.FullName)
            (match x.Spec with Some x -> x.ToString() | None -> "")
            (match x.StarWidth with Some x -> x.ToString() | None -> "")
            (match x.StarPrecision with Some x -> x.ToString() | None -> "")
            (x.FormatAsPrintF())

    /// Get the string representation that printf would have normally generated
    member x.FormatAsPrintF() =
        match type' with
        | PrintableElementType.FromFormatSpecifier -> printer ()
        | _ -> value :?> string
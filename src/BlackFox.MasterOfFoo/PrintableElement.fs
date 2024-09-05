namespace BlackFox.MasterOfFoo

open System
open FormatSpecifierConstants

/// The type of an element passed to a PrintfEnv.
type PrintableElementType =
    /// A string created by the engine, only used in a few edge cases
    | MadeByEngine = 0uy
    /// A string coming directly from the format string
    | Direct = 1uy
    /// A format specifier and his corresponding value(s)
    | FromFormatSpecifier = 2uy

/// An element passed to PrintfEnv for writing.
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

    /// The type of element (From a format specifier or directly from the string)
    member _.ElementType with get() = type'

    /// The value passed as parameter, of type ValueType
    member _.Value with get() = value

    /// The type of the value
    member _.ValueType
        with get() =
            match type' with
            | PrintableElementType.FromFormatSpecifier -> valueType
            | _ -> typeof<string>

    /// The format specification for format specifiers
    member _.Specifier with get() = if Object.ReferenceEquals(spec, null) then None else Some(spec)

    // The .Net format string for format specifiers

    /// The width if specified via another parameter as in "%*i"
    member _.StarWidth with get() = match starWidth with NotSpecifiedValue -> None | x -> Some(x)

    /// The precision if specified via another parameter as in "%.*f"
    member _.StarPrecision with get() = match starPrecision with NotSpecifiedValue -> None | x -> Some(x)

    override x.ToString () =
        // The .Net format is handled differently as it's stored in the Specifier but not present
        // in it's ToString() representation
        let dotnetFormat =
            match x.Specifier with
                | Some x ->
                    match x.InteropHoleDotNetFormat with
                        | ValueSome x -> Some x
                        | ValueNone -> None
                | None -> None
        let dotnetFormatAddition =
            match dotnetFormat with
                | Some x -> sprintf ", dotnetFormat: '%s'" x
                | None -> ""

        sprintf
            "value: %A, type: %A, valueType: %s, spec: %s%s, starWidth: %s, starPrecision: %s, AsPrintF: %s"
            value
            type'
            (x.ValueType.FullName)
            (match x.Specifier with Some x -> x.ToString() | None -> "")
            dotnetFormatAddition
            (match x.StarWidth with Some x -> x.ToString() | None -> "")
            (match x.StarPrecision with Some x -> x.ToString() | None -> "")
            (x.FormatAsPrintF())

    /// Get the string representation that printf would have normally generated
    member _.FormatAsPrintF() =
        match type' with
        | PrintableElementType.FromFormatSpecifier -> printer ()
        | _ -> value :?> string

    member internal _.IsNullOrEmpty with get() =
        match type' with
        | PrintableElementType.FromFormatSpecifier -> false
        | _ -> String.IsNullOrEmpty(value :?> string)

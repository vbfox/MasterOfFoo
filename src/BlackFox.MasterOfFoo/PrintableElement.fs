namespace MasterOfFoo

open System

type PrintableElementType =
    | MadeByEngine = 0
    | Direct = 1
    | FromFormatSpecifier = 2

type PrintableElement =
    struct
        val private printer: unit -> string
        member x.Printer with get() = x.printer

        val private value: obj
        member x.Value with get() = x.value

        val private type': PrintableElementType
        member x.Type with get() = x.type'

        val private valueType: Type
        member x.ValueType with get() = x.valueType

        val private spec: FormatSpecifier option
        member x.Spec with get() = x.spec

        new (printer, value: obj, type', valueType, spec) =
            {
                printer = printer
                value = value
                type' = type'
                valueType = valueType
                spec = spec
            }

        override x.ToString () = 
            sprintf
                "value: %A, type: %A, valueType: %s, spec: %s, AsPrintF: %s"
                x.value
                x.type'
                (x.valueType.FullName)
                (match x.spec with Some x -> x.ToString() | None -> "")
                (x.FormatAsPrintF())

        /// Get the string representation that printf would have normally generated
        member x.FormatAsPrintF() =
            x.printer ()

        static member inline MakeFromString(s: string, type': PrintableElementType) =
            let printer = fun () -> s
            PrintableElement(printer, s, type', typeof<string>, None)

        static member inline MadeByEngine(s: string) =
            PrintableElement.MakeFromString(s , PrintableElementType.MadeByEngine)

        static member MakeDirect(s: string) =
            PrintableElement.MakeFromString(s , PrintableElementType.Direct)

        static member MakeFromFormatSpecifier(printer: unit -> string, value: obj, valueType: Type, spec: FormatSpecifier) =
            PrintableElement(printer, value, PrintableElementType.FromFormatSpecifier, valueType, Some(spec))
    end
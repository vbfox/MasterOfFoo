namespace BlackFox.MasterOfFoo

open System

[<Flags>]
type FormatFlags = 
    | None = 0
    | LeftJustify = 1
    | PadWithZeros = 2
    | PlusForPositives = 4
    | SpaceForPositives = 8

module internal FormatSpecifierConstants =
    /// Used for width and precision to denote that user has specified '*' flag
    [<Literal>]
    let StarValue = -1

    /// Used for width and precision to denote that corresponding value was omitted in format string
    [<Literal>]
    let NotSpecifiedValue = -2

open FormatSpecifierConstants

[<System.Diagnostics.DebuggerDisplayAttribute("{ToString()}")>]
[<NoComparison; NoEquality>]
type FormatSpecifier =
    {
        TypeChar : char
        Precision : int
        Width : int
        Flags : FormatFlags
    }
    member this.IsStarPrecision = this.Precision = StarValue
    member this.IsPrecisionSpecified = this.Precision <> NotSpecifiedValue
    member this.IsStarWidth = this.Width = StarValue
    member this.IsWidthSpecified = this.Width <> NotSpecifiedValue

    override this.ToString() =
        let sb = System.Text.StringBuilder ("%")
        
        if this.Flags.HasFlag(FormatFlags.PadWithZeros) then sb.Append('0') |> ignore
        if this.Flags.HasFlag(FormatFlags.LeftJustify) then sb.Append('-') |> ignore
        if this.Flags.HasFlag(FormatFlags.PlusForPositives) then sb.Append('+') |> ignore
        if this.Flags.HasFlag(FormatFlags.SpaceForPositives) then sb.Append(' ') |> ignore
        
        let printValue n =
            match n with
            | StarValue -> sb.Append('*')  |> ignore
            | NotSpecifiedValue -> ()
            | n -> sb.Append(n.ToString())  |> ignore

        printValue this.Width
        if this.Precision <> NotSpecifiedValue then
            sb.Append('.') |> ignore
            printValue this.Precision

        sb.Append(this.TypeChar) |> ignore
        sb.ToString()
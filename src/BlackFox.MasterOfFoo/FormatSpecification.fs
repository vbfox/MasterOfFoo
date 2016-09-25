module MasterOfFoo.Core.FormatSpecification

open System

[<Flags>]
type FormatFlags = 
    | None = 0
    | LeftJustify = 1
    | PadWithZeros = 2
    | PlusForPositives = 4
    | SpaceForPositives = 8

let inline hasFlag flags (expected : FormatFlags) = (flags &&& expected) = expected
let inline isLeftJustify flags = hasFlag flags FormatFlags.LeftJustify
let inline isPadWithZeros flags = hasFlag flags FormatFlags.PadWithZeros
let inline isPlusForPositives flags = hasFlag flags FormatFlags.PlusForPositives
let inline isSpaceForPositives flags = hasFlag flags FormatFlags.SpaceForPositives

/// Used for width and precision to denote that user has specified '*' flag
[<Literal>]
let StarValue = -1
/// Used for width and precision to denote that corresponding value was omitted in format string
[<Literal>]
let NotSpecifiedValue = -2

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
        let valueOf n = match n with StarValue -> "*" | NotSpecifiedValue -> "-" | n -> n.ToString()
        System.String.Format
            (
                "'{0}', Precision={1}, Width={2}, Flags={3}", 
                this.TypeChar, 
                (valueOf this.Precision),
                (valueOf this.Width), 
                this.Flags
            )
    

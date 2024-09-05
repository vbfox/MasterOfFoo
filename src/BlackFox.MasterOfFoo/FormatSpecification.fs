namespace BlackFox.MasterOfFoo

open System

[<Flags>]
type FormatFlags =
    | None = 0
    | LeftJustify = 1
    | PadWithZeros = 2
    | PlusForPositives = 4
    | SpaceForPositives = 8

module internal FormatFlagsHelpers =
    let inline hasFlag flags (expected: FormatFlags) = (flags &&& expected) = expected
    let inline isLeftJustify flags = hasFlag flags FormatFlags.LeftJustify
    let inline isPadWithZeros flags = hasFlag flags FormatFlags.PadWithZeros
    let inline isPlusForPositives flags = hasFlag flags FormatFlags.PlusForPositives
    let inline isSpaceForPositives flags = hasFlag flags FormatFlags.SpaceForPositives

open FormatFlagsHelpers

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
        TypeChar: char
        Precision: int
        Width: int
        Flags: FormatFlags
        InteropHoleDotNetFormat: string voption
    }
    member spec.IsStarPrecision = (spec.Precision = StarValue)

    member spec.IsPrecisionSpecified = (spec.Precision <> NotSpecifiedValue)

    member spec.IsStarWidth = (spec.Width = StarValue)

    member spec.IsWidthSpecified = (spec.Width <> NotSpecifiedValue)

    member spec.ArgCount =
        let n =
            if spec.TypeChar = 'a' then 2
            elif spec.IsStarWidth || spec.IsStarPrecision then
                if spec.IsStarWidth = spec.IsStarPrecision then 3
                else 2
            else 1

        let n = if spec.TypeChar = '%' then n - 1 else n

        assert (n <> 0)

        n

    override spec.ToString() =
        let valueOf n = match n with StarValue -> "*" | NotSpecifiedValue -> "-" | n -> n.ToString()
        System.String.Format
            (
                "'{0}', Precision={1}, Width={2}, Flags={3}",
                spec.TypeChar,
                (valueOf spec.Precision),
                (valueOf spec.Width),
                spec.Flags
            )

    member spec.IsDecimalFormat =
        spec.TypeChar = 'M'

    member spec.GetPadAndPrefix allowZeroPadding =
        let padChar = if allowZeroPadding && isPadWithZeros spec.Flags then '0' else ' ';
        let prefix =
            if isPlusForPositives spec.Flags then "+"
            elif isSpaceForPositives spec.Flags then " "
            else ""
        padChar, prefix

    member spec.IsGFormat =
        spec.IsDecimalFormat || System.Char.ToLower(spec.TypeChar) = 'g'

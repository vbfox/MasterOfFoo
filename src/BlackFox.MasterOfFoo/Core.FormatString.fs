/// Set of helpers to parse format string
module internal BlackFox.MasterOfFoo.Core.FormatString

open BlackFox.MasterOfFoo
open BlackFox.MasterOfFoo.FormatSpecifierConstants
open System

let inline isDigit c = c >= '0' && c <= '9'
let intFromString (s : string) pos = 
    let rec go acc i =
        if isDigit s.[i] then 
            let n = int s.[i] - int '0'
            go (acc * 10 + n) (i + 1)
        else acc, i
    go 0 pos

let parseFlags (s : string) i : FormatFlags * int = 
    let rec go flags i = 
        match s.[i] with
        | '0' -> go (flags ||| FormatFlags.PadWithZeros) (i + 1)
        | '+' -> go (flags ||| FormatFlags.PlusForPositives) (i + 1)
        | ' ' -> go (flags ||| FormatFlags.SpaceForPositives) (i + 1)
        | '-' -> go (flags ||| FormatFlags.LeftJustify) (i + 1)
        | _ -> flags, i
    go FormatFlags.None i

let parseWidth (s : string) i : int * int = 
    if s.[i] = '*' then StarValue, (i + 1)
    elif isDigit (s.[i]) then intFromString s i
    else NotSpecifiedValue, i

let parsePrecision (s : string) i : int * int = 
    if s.[i] = '.' then
        if s.[i + 1] = '*' then StarValue, i + 2
        elif isDigit (s.[i + 1]) then intFromString s (i + 1)
        else raise (ArgumentException("invalid precision value"))
    else NotSpecifiedValue, i
        
let parseTypeChar (s : string) i : char * int = 
    s.[i], (i + 1)

let findNextFormatSpecifier (s : string) i = 
    let rec go i (buf : Text.StringBuilder) =
        if i >= s.Length then 
            s.Length, PrintableElement(buf.ToString(), PrintableElementType.Direct)
        else
            let c = s.[i]
            if c = '%' then
                if i + 1 < s.Length then
                    let _, i1 = parseFlags s (i + 1)
                    let w, i2 = parseWidth s i1
                    let p, i3 = parsePrecision s i2
                    let typeChar, i4 = parseTypeChar s i3
                    // shortcut for the simpliest case
                    // if typeChar is not % or it has star as width\precision - resort to long path
                    if typeChar = '%' && not (w = StarValue || p = StarValue) then 
                        buf.Append('%') |> ignore
                        go i4 buf
                    else 
                        i, PrintableElement(buf.ToString(), PrintableElementType.Direct)
                else
                    raise (ArgumentException("Missing format specifier"))
            else 
                buf.Append(c) |> ignore
                go (i + 1) buf
    go i (Text.StringBuilder())
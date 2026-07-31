/// The %B binary specifier, added by F# 6.0 / FSharp.Core 6.0 (RFC FS-1100).
module Cases.Binary60

open Common

let run () =
    section "Binary60"

    check "B" (mysprintf "%B" 42) (sprintf "%B" 42)
    check "B zero" (mysprintf "%B" 0) (sprintf "%B" 0)
    check "B byte" (mysprintf "%B" 255uy) (sprintf "%B" 255uy)
    check "B int64" (mysprintf "%B" 9000000000L) (sprintf "%B" 9000000000L)
    check "B width" (mysprintf "%8B" 5) (sprintf "%8B" 5)
    check "B zero pad" (mysprintf "%08B" 5) (sprintf "%08B" 5)
    check "B left justify" (mysprintf "%-8B" 5) (sprintf "%-8B" 5)
    check "B star width" (mysprintf "%*B" 8 5) (sprintf "%*B" 8 5)
    check "B interpolated" (mysprintf $"%B{42}") (sprintf $"%B{42}")

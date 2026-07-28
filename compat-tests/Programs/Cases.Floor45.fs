/// Format features available since the oldest FSharp.Core the package supports
/// (4.5.0). No string interpolation, no %B.
module Cases.Floor45

open Common

type Union =
    | A of string
    | B of int

type SaysHi() =
    override _.ToString() = "Hi"

let run () =
    section "Floor45"

    // Plain text and the literal percent escape
    check "text" (mysprintf "Hello") (sprintf "Hello")
    check "literal percent" (mysprintf "100%%") (sprintf "100%%")

    // Simple specifiers
    check "s" (mysprintf "%s" "Foo") (sprintf "%s" "Foo")
    check "b" (mysprintf "%b" true) (sprintf "%b" true)
    check "c" (mysprintf "%c" 'x') (sprintf "%c" 'x')

    // Integers
    check "i" (mysprintf "%i" 42) (sprintf "%i" 42)
    check "d negative" (mysprintf "%d" -42) (sprintf "%d" -42)
    check "u" (mysprintf "%u" 42) (sprintf "%u" 42)
    check "x" (mysprintf "%x" 255) (sprintf "%x" 255)
    check "X" (mysprintf "%X" 255) (sprintf "%X" 255)
    check "o" (mysprintf "%o" 42) (sprintf "%o" 42)
    check "i int64" (mysprintf "%i" 9000000000L) (sprintf "%i" 9000000000L)

    // Floats and decimal
    check "f" (mysprintf "%f" 42.42) (sprintf "%f" 42.42)
    check "F" (mysprintf "%F" 42.42) (sprintf "%F" 42.42)
    check "e" (mysprintf "%e" 42.42) (sprintf "%e" 42.42)
    check "E" (mysprintf "%E" 42.42) (sprintf "%E" 42.42)
    check "g" (mysprintf "%g" 42.42) (sprintf "%g" 42.42)
    check "G" (mysprintf "%G" 42.42) (sprintf "%G" 42.42)
    check "f float32" (mysprintf "%f" 42.0f) (sprintf "%f" 42.0f)
    check "M" (mysprintf "%M" 123456789.123456789M) (sprintf "%M" 123456789.123456789M)

    // Object and structured formatting
    check "O" (mysprintf "%O" (SaysHi())) (sprintf "%O" (SaysHi()))
    check "A string" (mysprintf "%A" "Foo") (sprintf "%A" "Foo")
    check "A int" (mysprintf "%A" 5) (sprintf "%A" 5)
    check "A union" (mysprintf "%A" (A "Foo")) (sprintf "%A" (A "Foo"))
    check "A union int" (mysprintf "%A" (B 42)) (sprintf "%A" (B 42))
    check "A option" (mysprintf "%A" (Some true)) (sprintf "%A" (Some true))
    check "A list" (mysprintf "%A" [ 1; 2; 3 ]) (sprintf "%A" [ 1; 2; 3 ])
    check "A enum" (mysprintf "%A" System.ConsoleColor.Red) (sprintf "%A" System.ConsoleColor.Red)

    // Flags
    check "plus" (mysprintf "%+i" 42) (sprintf "%+i" 42)
    check "space" (mysprintf "% i" 42) (sprintf "% i" 42)
    check "zero pad" (mysprintf "%05i" 42) (sprintf "%05i" 42)
    check "left justify int" (mysprintf "%-5i" 42) (sprintf "%-5i" 42)
    check "left justify string" (mysprintf "%-5s" "Foo") (sprintf "%-5s" "Foo")

    // Width and precision
    check "width 5 string" (mysprintf "%5s" "Foo") (sprintf "%5s" "Foo")
    check "width 1 string" (mysprintf "%1s" "Foo") (sprintf "%1s" "Foo")
    check "width 5 int" (mysprintf "%5i" 42) (sprintf "%5i" 42)
    check "precision" (mysprintf "%.3f" 42.123456) (sprintf "%.3f" 42.123456)
    check "width precision" (mysprintf "%10.3f" 42.123456) (sprintf "%10.3f" 42.123456)
    check "left width precision" (mysprintf "%-10.3f" 42.123456) (sprintf "%-10.3f" 42.123456)

    // Star width and precision
    check "star width string" (mysprintf "%*s" 5 "Foo") (sprintf "%*s" 5 "Foo")
    check "star width left" (mysprintf "%-*s" 5 "Foo") (sprintf "%-*s" 5 "Foo")
    check "star width int" (mysprintf "%*i" 5 42) (sprintf "%*i" 5 42)
    check "star precision" (mysprintf "%.*f" 3 42.123456) (sprintf "%.*f" 3 42.123456)
    check "star width and precision" (mysprintf "%*.*f" 10 3 42.123456) (sprintf "%*.*f" 10 3 42.123456)

    // Function specifiers
    check "t" (mysprintf "%t" (fun () -> "T")) (sprintf "%t" (fun () -> "T"))
    check "a" (mysprintf "%a" (fun () (v: int) -> string v) 42) (sprintf "%a" (fun () (v: int) -> string v) 42)

    // Several holes mixed with text
    check "mixed" (mysprintf "a%sb%ic%5.2fd" "S" 42 3.14159) (sprintf "a%sb%ic%5.2fd" "S" 42 3.14159)

    check
        "chained"
        (mysprintf "%s %s %s %s %s %s" "1" "2" "3" "4" "5" "6")
        (sprintf "%s %s %s %s %s %s" "1" "2" "3" "4" "5" "6")

module Common

open System
open System.Globalization
open System.Text
open System.Threading

open BlackFox.MasterOfFoo

/// Reproduces what sprintf would produce, through the library's public extension
/// point. Same shape as the sample in the project Readme.
type private SprintfEnv() =
    inherit PrintfEnv<unit, string, string>()
    let buf = StringBuilder()
    override _.Finalize() = buf.ToString()
    override _.Write(s: PrintableElement) = ignore (buf.Append(s.FormatAsPrintF()))
    override _.WriteT(s: string) = ignore (buf.Append s)

let mysprintf (format: Printf.StringFormat<'T>) =
    doPrintfFromEnv format (SprintfEnv())

let section (name: string) = Console.Out.WriteLine("-- {0} --", name)

/// FSharp.Core's own sprintf is the oracle: `actual` comes from the library,
/// `expected` from whichever FSharp.Core version this program was built against.
/// A mismatch stays visible in the output instead of throwing, so a single run
/// reports every broken specifier rather than only the first one.
let check (label: string) (actual: string) (expected: string) =
    if actual = expected then
        Console.Out.WriteLine("{0} | {1}", label, actual)
    else
        Console.Out.WriteLine("{0} | {1} | MISMATCH core={2}", label, actual, expected)

/// Some specifiers (%f, %M, .NET interpolation formats) render differently per
/// culture, so pin it the way the Expecto suite does before producing output.
let runProgram (body: unit -> unit) =
    Thread.CurrentThread.CurrentCulture <- CultureInfo.InvariantCulture
    Thread.CurrentThread.CurrentUICulture <- CultureInfo.InvariantCulture
    body ()
    0

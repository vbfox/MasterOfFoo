// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System.Text
open BlackFox.MasterOfFoo
open System.Data.SqlClient
open System.Data.Common

module ReimplementPrintf =
    open System
    type StringPrintfEnv<'Result>(k, n) = 
        inherit PrintfEnv<unit, string, 'Result>(())

        let buf : string[] = Array.zeroCreate n
        let mutable ptr = 0

        override this.Finalize() : 'Result = k (String.Concat(buf))
        override this.Write(s : PrintableElement) = 
            buf.[ptr] <- s.ToString()
            ptr <- ptr + 1
        override this.WriteT(s) = 
            buf.[ptr] <- s
            ptr <- ptr + 1

    type StringBuilderPrintfEnv<'Result>(k, buf) = 
        inherit PrintfEnv<Text.StringBuilder, unit, 'Result>(buf)
        override this.Finalize() : 'Result = k ()
        override this.Write(s : PrintableElement) = ignore(buf.Append(s.ToString()))
        override this.WriteT(()) = ()

    type TextWriterPrintfEnv<'Result>(k, tw : IO.TextWriter) =
        inherit PrintfEnv<IO.TextWriter, unit, 'Result>(tw)
        override this.Finalize() : 'Result = k()
        override this.Write(s : PrintableElement) = tw.Write (s.ToString())
        override this.WriteT(()) = ()

    type BuilderFormat<'T,'Result>    = Format<'T, System.Text.StringBuilder, unit, 'Result>
    type StringFormat<'T,'Result>     = Format<'T, unit, string, 'Result>
    type TextWriterFormat<'T,'Result> = Format<'T, System.IO.TextWriter, unit, 'Result>
    type BuilderFormat<'T>     = BuilderFormat<'T,unit>
    type StringFormat<'T>      = StringFormat<'T,string>
    type TextWriterFormat<'T>  = TextWriterFormat<'T,unit>

    [<CompiledName("PrintFormatToStringThen")>]
    let ksprintf continuation (format : Format<'T, unit, string, 'Result>) : 'T = 
        doPrintf format (fun n -> StringPrintfEnv(continuation, n))

    [<CompiledName("PrintFormatToStringThen")>]
    let sprintf (format : StringFormat<'T>)  = ksprintf id format

    [<CompiledName("PrintFormatThen")>]
    let kprintf f fmt = ksprintf f fmt

    [<CompiledName("PrintFormatToStringBuilderThen")>]
    let kbprintf f (buf: System.Text.StringBuilder) fmt = 
        doPrintfFromEnv fmt (StringBuilderPrintfEnv(f, buf))
    
    [<CompiledName("PrintFormatToTextWriterThen")>]
    let kfprintf f os fmt = doPrintfFromEnv fmt (TextWriterPrintfEnv(f, os))

    [<CompiledName("PrintFormatToStringBuilder")>]
    let bprintf buf fmt  = kbprintf ignore buf fmt 

    [<CompiledName("PrintFormatToTextWriter")>]
    let fprintf (os: System.IO.TextWriter) fmt  = kfprintf ignore os fmt 

    [<CompiledName("PrintFormatLineToTextWriter")>]
    let fprintfn (os: System.IO.TextWriter) fmt  = kfprintf (fun _ -> os.WriteLine()) os fmt

    [<CompiledName("PrintFormatToStringThenFail")>]
    let failwithf fmt = ksprintf failwith fmt

    [<CompiledName("PrintFormat")>]
    let printf fmt = fprintf System.Console.Out fmt

    [<CompiledName("PrintFormatToError")>]
    let eprintf fmt = fprintf System.Console.Error fmt

    [<CompiledName("PrintFormatLine")>]
    let printfn fmt = fprintfn System.Console.Out fmt

    [<CompiledName("PrintFormatLineToError")>]
    let eprintfn fmt = fprintfn System.Console.Error fmt

type internal SqlEnv<'cmd when 'cmd :> DbCommand>(n: int, command: 'cmd) =
    inherit PrintfEnv<unit, unit, 'cmd>(())
    let queryString = StringBuilder()
    let mutable index = 0

    let addParameter (p: DbParameter) =
        ignore(queryString.Append p.ParameterName)
        command.Parameters.Add p |> ignore

    override __.Finalize() =
        command.CommandText <- queryString.ToString()
        command

    override __.Write(s : PrintableElement) =
        let asPrintf = s.FormatAsPrintF()
        match s.ElementType with
        | PrintableElementType.FromFormatSpecifier ->
            let parameter =
                if typeof<DbParameter>.IsAssignableFrom(s.ValueType) then
                    s.Value :?> DbParameter
                else
                    let paramName = sprintf "@p%i" index
                    index <- index + 1

                    let parameter = command.CreateParameter()
                    parameter.ParameterName <- paramName
                    parameter.Value <- s.Value
                    parameter

            addParameter parameter
        | _ ->
            ignore(queryString.Append asPrintf)

    override __.WriteT(()) = ()

let sqlCommandf (format : Format<'T, unit, unit, SqlCommand>) =
    MasterOfFoo.doPrintf format (fun n -> SqlEnv(n, new SqlCommand ()) :> PrintfEnv<_, _, _>)

module ColorPrintf =
    open System
    open System.Text
    open BlackFox.MasterOfFoo

    type private Colorize<'Result>(k) =
        inherit PrintfEnv<unit, string, 'Result>()
        override this.Finalize() : 'Result = k()
        override this.Write(s : PrintableElement) =
            match s.ElementType with
            | PrintableElementType.FromFormatSpecifier -> 
                let color = Console.ForegroundColor
                Console.ForegroundColor <- ConsoleColor.Blue
                Console.Write(s.FormatAsPrintF())
                Console.ForegroundColor <- color
            | _ -> Console.Write(s.FormatAsPrintF())
        override this.WriteT(s : string) =
            let color = Console.ForegroundColor
            Console.ForegroundColor <- ConsoleColor.Red
            Console.Write(s)
            Console.ForegroundColor <- color

    let colorprintf (format: Format<'T, unit, string, unit>) =
        doPrintfFromEnv format (Colorize id)

//MyModule.mysprintf "Hello %s.\n" "World"

type internal QueryStringEnv() =
    inherit PrintfEnv<StringBuilder, unit, string>(StringBuilder())

    override this.Finalize() = this.State.ToString()

    override this.Write(s : PrintableElement) =
        let asPrintf = s.FormatAsPrintF()
        match s.ElementType with
        | PrintableElementType.FromFormatSpecifier -> 
            let escaped = System.Uri.EscapeDataString(asPrintf)
            ignore(this.State.Append escaped)
        | _ ->
            ignore(this.State.Append asPrintf)

    override this.WriteT(()) = ()

let queryStringf (format : Format<'T, StringBuilder, unit, string>) =
    MasterOfFoo.doPrintf format (fun _ -> QueryStringEnv() :> PrintfEnv<_, _, _>)

type internal MyTestEnv<'Result>(k, state) = 
    inherit PrintfEnv<StringBuilder, unit, 'Result>(state)
    override this.Finalize() : 'Result =
        printfn "Finalizing"
        k ()
    override this.Write(s : PrintableElement) =
        printfn "Writing: %A" s
        state.Append(s.FormatAsPrintF()) |> ignore
    override this.WriteT(()) = 
        printfn "WTF"

let testprintf (sb: StringBuilder) (format : Format<'T, StringBuilder, unit, unit>) =
    MasterOfFoo.doPrintf format (fun n -> 
        MyTestEnv(ignore, sb) :> PrintfEnv<_, _, _>
    )

let simple () =
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello %0-10i hello %s %A" 1000 "World" "World"
    sb.Clear() |> ignore
    testprintf sb "Hello %-010i hello %s" 1000 "World"
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

let percentStar () =
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello '%*i'" 5 42
    sb.Clear() |> ignore
    testprintf sb "Hello '%.*f'" 3 42.12345
    sb.Clear() |> ignore
    testprintf sb "Hello '%*.*f'" 5 3 42.12345
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

let chained () =
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello %s %s %s %s %s %s" "1" "2" "3" "4" "5" "6"
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

let complex () = 
    printfn "------------------------------------------------"
    let sb = StringBuilder ()
    testprintf sb "Hello %s %s %s %s %s %s %06i %t" "1" "2" "3" "4" "5" "6" 5 (fun x -> x.Append("CALLED") |> ignore)
    System.Console.WriteLine("RESULT: {0}", sb.ToString())

[<EntryPoint>]
let main argv =
    //printfn "%s" (queryStringf "hello/uri+with space/x?foo=%s&bar=%s&work=%i" "#baz" "++Hello world && problem for arg √" 1)

    let cmd: SqlCommand =
        sqlCommandf
            "SELECT * FROM tbUser WHERE UserId=%i AND NAME=%s AND CREATIONDATE > %O"
            5
            "Test"
            System.DateTimeOffset.Now
    ColorPrintf.colorprintf "%s est %t" "La vie" (fun _ -> "belle !")
    // simple ()
    //percentStar ()
    //chained ()
    //complex ()
    ignore(System.Console.ReadLine ())
    0 // return an integer exit code

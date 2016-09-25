[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MasterOfFoo.Core.Printf 

open System
open PrintfImpl

type StringPrintfEnv<'Result>(k, n) = 
    inherit PrintfEnv<unit, string, 'Result>(())

    let buf : string[] = Array.zeroCreate n
    let mutable ptr = 0

    override this.Finalize() : 'Result = k (String.Concat(buf))
    override this.Write(s : string) = 
        buf.[ptr] <- s
        ptr <- ptr + 1
    override this.WriteT(s) = this.Write s

type StringBuilderPrintfEnv<'Result>(k, buf) = 
    inherit PrintfEnv<Text.StringBuilder, unit, 'Result>(buf)
    override this.Finalize() : 'Result = k ()
    override this.Write(s : string) = ignore(buf.Append(s))
    override this.WriteT(()) = ()

type TextWriterPrintfEnv<'Result>(k, tw : IO.TextWriter) =
    inherit PrintfEnv<IO.TextWriter, unit, 'Result>(tw)
    override this.Finalize() : 'Result = k()
    override this.Write(s : string) = tw.Write s
    override this.WriteT(()) = ()

type BuilderFormat<'T,'Result>    = Format<'T, System.Text.StringBuilder, unit, 'Result>
type StringFormat<'T,'Result>     = Format<'T, unit, string, 'Result>
type TextWriterFormat<'T,'Result> = Format<'T, System.IO.TextWriter, unit, 'Result>
type BuilderFormat<'T>     = BuilderFormat<'T,unit>
type StringFormat<'T>      = StringFormat<'T,string>
type TextWriterFormat<'T>  = TextWriterFormat<'T,unit>

[<CompiledName("PrintFormatToStringThen")>]
let ksprintf continuation (format : Format<'T, unit, string, 'Result>) : 'T = 
    doPrintf format (fun n -> 
        StringPrintfEnv(continuation, n) :> PrintfEnv<_, _, _>
    )

[<CompiledName("PrintFormatToStringThen")>]
let sprintf (format : StringFormat<'T>)  = ksprintf id format

[<CompiledName("PrintFormatThen")>]
let kprintf f fmt = ksprintf f fmt

[<CompiledName("PrintFormatToStringBuilderThen")>]
let kbprintf f (buf: System.Text.StringBuilder) fmt = 
    doPrintf fmt (fun _ -> 
        StringBuilderPrintfEnv(f, buf) :> PrintfEnv<_, _, _> 
    )
    
[<CompiledName("PrintFormatToTextWriterThen")>]
let kfprintf f os fmt =
    doPrintf fmt (fun _ -> 
        TextWriterPrintfEnv(f, os) :> PrintfEnv<_, _, _>
    )

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
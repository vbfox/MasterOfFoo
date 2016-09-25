namespace Microsoft.FSharp.Text.StructuredPrintfImpl

    /// These are a typical set of options used to control structured formatting.
    [<NoEquality; NoComparison>]
    type FormatOptions = 
        { FloatingPointFormat: string;
          AttributeProcessor: (string -> (string * string) list -> bool -> unit);
          FormatProvider: System.IFormatProvider;
#if FX_RESHAPED_REFLECTION
          ShowNonPublic : bool
#else
          BindingFlags: System.Reflection.BindingFlags
#endif
          PrintWidth : int; 
          PrintDepth : int; 
          PrintLength : int;
          PrintSize : int;        
          ShowProperties : bool;
          ShowIEnumerable: bool; }
        static member Default =
            { FormatProvider = (System.Globalization.CultureInfo.InvariantCulture :> System.IFormatProvider);
              AttributeProcessor= (fun _ _ _ -> ());
#if FX_RESHAPED_REFLECTION
              ShowNonPublic = false
#else
              BindingFlags = System.Reflection.BindingFlags.Public;
#endif
              FloatingPointFormat = "g10";
              PrintWidth = 80 ; 
              PrintDepth = 100 ; 
              PrintLength = 100;
              PrintSize = 10000;
              ShowProperties = false;
              ShowIEnumerable = true; }

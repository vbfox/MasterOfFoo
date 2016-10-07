namespace BlackFox.MasterOfFoo.Core

/// These are a typical set of options used to control structured formatting.
[<NoEquality; NoComparison>]
type internal FormatOptions =
    {
        FloatingPointFormat: string;
        AttributeProcessor: (string -> (string * string) list -> bool -> unit);
        FormatProvider: System.IFormatProvider;
        BindingFlags: System.Reflection.BindingFlags
        PrintWidth : int; 
        PrintDepth : int; 
        PrintLength : int;
        PrintSize : int;        
        ShowProperties : bool;
        ShowIEnumerable: bool;
    }

    static member Default =
        { FormatProvider = (System.Globalization.CultureInfo.InvariantCulture :> System.IFormatProvider);
            AttributeProcessor= (fun _ _ _ -> ());
            BindingFlags = System.Reflection.BindingFlags.Public;
            FloatingPointFormat = "g10";
            PrintWidth = 80 ; 
            PrintDepth = 100 ; 
            PrintLength = 100;
            PrintSize = 10000;
            ShowProperties = false;
            ShowIEnumerable = true; }

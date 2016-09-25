module MasterOfFoo.Core.FormatToString

open MasterOfFoo.Core.FormatSpecification
open System
open System.Reflection

let inline boolToString v = if v then "true" else "false"
let inline stringToSafeString v = if v = null then "" else v

[<Literal>]
let DefaultPrecision = 6

let getFormatForFloat (ch : char) (prec : int) = ch.ToString() +  prec.ToString()
let normalizePrecision prec = min (max prec 0) 99

/// Contains helpers to convert printer functions to functions that prints value with respect to specified justification
/// There are two kinds to printers: 
/// 'T -> string - converts value to string - used for strings, basic integers etc..
/// string -> 'T -> string - converts value to string with given format string - used by numbers with floating point, typically precision is set via format string 
/// To support both categories there are two entry points:
/// - withPadding - adapts first category
/// - withPaddingFormatted - adapts second category
module Padding = 
    /// pad here is function that converts T to string with respect of justification
    /// basic - function that converts T to string without appying justification rules
    /// adaptPaddedFormatted returns boxed function that has various number of arguments depending on if width\precision flags has '*' value 
    let inline adaptPaddedFormatted (spec : FormatSpecifier) getFormat (basic : string -> 'T -> string) (pad : string -> int -> 'T -> string) = 
        if spec.IsStarWidth then
            if spec.IsStarPrecision then
                // width=*, prec=*
                box(fun v width prec -> 
                    let fmt = getFormat (normalizePrecision prec)
                    pad fmt width v
                    )
            else 
                // width=*, prec=?
                let prec = if spec.IsPrecisionSpecified then normalizePrecision spec.Precision else DefaultPrecision
                let fmt = getFormat prec
                box(fun v width -> 
                    pad fmt width v
                    )
        elif spec.IsStarPrecision then
            if spec.IsWidthSpecified then
                // width=val, prec=*
                box(fun v prec -> 
                    let fmt = getFormat prec
                    pad fmt spec.Width v
                    )
            else
                // width=X, prec=*
                box(fun v prec -> 
                    let fmt = getFormat prec
                    basic fmt v
                    )                        
        else
            let prec = if spec.IsPrecisionSpecified then normalizePrecision spec.Precision else DefaultPrecision
            let fmt = getFormat prec
            if spec.IsWidthSpecified then
                // width=val, prec=*
                box(fun v -> 
                    pad fmt spec.Width v
                    )
            else
                // width=X, prec=*
                box(fun v -> 
                    basic fmt v
                    )

    /// pad here is function that converts T to string with respect of justification
    /// basic - function that converts T to string without appying justification rules
    /// adaptPadded returns boxed function that has various number of arguments depending on if width flags has '*' value 
    let inline adaptPadded (spec : FormatSpecifier) (basic : 'T -> string) (pad : int -> 'T -> string) = 
        if spec.IsStarWidth then
                // width=*, prec=?
                box(fun v width -> 
                    pad width v
                    )
        else
            if spec.IsWidthSpecified then
                // width=val, prec=*
                box(fun v -> 
                    pad spec.Width v
                    )
            else
                // width=X, prec=*
                box(fun v -> 
                    basic v
                    )

    let inline withPaddingFormatted (spec : FormatSpecifier) getFormat  (defaultFormat : string) (f : string ->  'T -> string) left right =
        if not (spec.IsWidthSpecified || spec.IsPrecisionSpecified) then
            box (f defaultFormat)
        else
            if isLeftJustify spec.Flags then
                adaptPaddedFormatted spec getFormat f left
            else
                adaptPaddedFormatted spec getFormat f right

    let inline withPadding (spec : FormatSpecifier) (f : 'T -> string) left right =
        if not (spec.IsWidthSpecified) then
            box f
        else
            if isLeftJustify spec.Flags then
                adaptPadded spec f left
            else
                adaptPadded  spec f right

let inline isNumber (x: ^T) =
    not (^T: (static member IsPositiveInfinity: 'T -> bool) x) && not (^T: (static member IsNegativeInfinity: 'T -> bool) x) && not (^T: (static member IsNaN: 'T -> bool) x)

let inline isInteger n = 
    n % LanguagePrimitives.GenericOne = LanguagePrimitives.GenericZero
    
let inline isPositive n = 
    n >= LanguagePrimitives.GenericZero

/// contains functions to handle left\right justifications for non-numeric types (strings\bools)
module Basic =
    let inline leftJustify f padChar = 
        fun (w : int) v -> 
            (f v : string).PadRight(w, padChar)
    
    let inline rightJustify f padChar = 
        fun (w : int) v -> 
            (f v : string).PadLeft(w, padChar)
    
    
/// contains functions to handle left\right and no justification case for numbers
module GenericNumber =
    /// handles right justification when pad char = '0'
    /// this case can be tricky:
    /// - negative numbers, -7 should be printed as '-007', not '00-7'
    /// - positive numbers when prefix for positives is set: 7 should be '+007', not '00+7'
    let inline rightJustifyWithZeroAsPadChar (str : string) isNumber isPositive w (prefixForPositives : string) =
        System.Diagnostics.Debug.Assert(prefixForPositives.Length = 0 || prefixForPositives.Length = 1)
        if isNumber then
            if isPositive then
                prefixForPositives + (if w = 0 then str else str.PadLeft(w - prefixForPositives.Length, '0')) // save space to 
            else
                if str.[0] = '-' then
                    let str = str.Substring(1)
                    "-" + (if w = 0 then str else str.PadLeft(w - 1, '0'))
                else
                    str.PadLeft(w, '0')
        else
            str.PadLeft(w, ' ')
        
    /// handler right justification when pad char = ' '
    let inline rightJustifyWithSpaceAsPadChar (str : string) isNumber isPositive w (prefixForPositives : string) =
        System.Diagnostics.Debug.Assert(prefixForPositives.Length = 0 || prefixForPositives.Length = 1)
        (if isNumber && isPositive then prefixForPositives + str else str).PadLeft(w, ' ')
        
    /// handles left justification with formatting with 'G'\'g' - either for decimals or with 'g'\'G' is explicitly set 
    let inline leftJustifyWithGFormat (str : string) isNumber isInteger isPositive w (prefixForPositives : string) padChar  =
        if isNumber then
            let str = if isPositive then prefixForPositives + str else str
            // NOTE: difference - for 'g' format we use isInt check to detect situations when '5.0' is printed as '5'
            // in this case we need to override padding and always use ' ', otherwise we'll produce incorrect results
            if isInteger then
                str.PadRight(w, ' ') // don't pad integer numbers with '0' when 'g' format is specified (may yield incorrect results)
            else
                str.PadRight(w, padChar) // non-integer => string representation has point => can pad with any character
        else
            str.PadRight(w, ' ') // pad NaNs with ' '

    let inline leftJustifyWithNonGFormat (str : string) isNumber isPositive w (prefixForPositives : string) padChar  =
        if isNumber then
            let str = if isPositive then prefixForPositives + str else str
            str.PadRight(w, padChar)
        else
            str.PadRight(w, ' ') // pad NaNs with ' ' 
        
    /// processes given string based depending on values isNumber\isPositive
    let inline noJustificationCore (str : string) isNumber isPositive prefixForPositives = 
        if isNumber && isPositive then prefixForPositives + str
        else str
        
    /// noJustification handler for f : 'T -> string - basic integer types
    let inline noJustification f (prefix : string) isUnsigned =
        if isUnsigned then
            fun v -> noJustificationCore (f v) true true prefix
        else 
            fun v -> noJustificationCore (f v) true (isPositive v) prefix

    /// noJustification handler for f : string -> 'T -> string - floating point types
    let inline noJustificationWithFormat f (prefix : string) = 
        fun (fmt : string) v -> noJustificationCore (f fmt v) true (isPositive v) prefix

    /// leftJustify handler for f : 'T -> string - basic integer types
    let inline leftJustify isGFormat f (prefix : string) padChar isUnsigned = 
        if isUnsigned then
            if isGFormat then
                fun (w : int) v ->
                    leftJustifyWithGFormat (f v) true (isInteger v) true w prefix padChar
            else
                fun (w : int) v ->
                    leftJustifyWithNonGFormat (f v) true true w prefix padChar
        else
            if isGFormat then
                fun (w : int) v ->
                    leftJustifyWithGFormat (f v) true (isInteger v) (isPositive v) w prefix padChar
            else
                fun (w : int) v ->
                    leftJustifyWithNonGFormat (f v) true (isPositive v) w prefix padChar
        
    /// leftJustify handler for f : string -> 'T -> string - floating point types                    
    let inline leftJustifyWithFormat isGFormat f (prefix : string) padChar = 
        if isGFormat then
            fun (fmt : string) (w : int) v ->
                leftJustifyWithGFormat (f fmt v) true (isInteger v) (isPositive v) w prefix padChar
        else
            fun (fmt : string) (w : int) v ->
                leftJustifyWithNonGFormat (f fmt v) true (isPositive v) w prefix padChar    

    /// rightJustify handler for f : 'T -> string - basic integer types
    let inline rightJustify f (prefixForPositives : string) padChar isUnsigned =
        if isUnsigned then
            if padChar = '0' then
                fun (w : int) v ->
                    rightJustifyWithZeroAsPadChar (f v) true true w prefixForPositives
            else
                System.Diagnostics.Debug.Assert((padChar = ' '))
                fun (w : int) v ->
                    rightJustifyWithSpaceAsPadChar (f v) true true w prefixForPositives
        else
            if padChar = '0' then
                fun (w : int) v ->
                    rightJustifyWithZeroAsPadChar (f v) true (isPositive v) w prefixForPositives

            else
                System.Diagnostics.Debug.Assert((padChar = ' '))
                fun (w : int) v ->
                    rightJustifyWithSpaceAsPadChar (f v) true (isPositive v) w prefixForPositives

    /// rightJustify handler for f : string -> 'T -> string - floating point types                    
    let inline rightJustifyWithFormat f (prefixForPositives : string) padChar =
        if padChar = '0' then
            fun (fmt : string) (w : int) v ->
                rightJustifyWithZeroAsPadChar (f fmt v) true (isPositive v) w prefixForPositives

        else
            System.Diagnostics.Debug.Assert((padChar = ' '))
            fun (fmt : string) (w : int) v ->
                rightJustifyWithSpaceAsPadChar (f fmt v) true (isPositive v) w prefixForPositives
module Float = 
    let inline noJustification f (prefixForPositives : string) = 
        fun (fmt : string) v -> 
            GenericNumber.noJustificationCore (f fmt v) (isNumber v) (isPositive v) prefixForPositives
    
    let inline leftJustify isGFormat f (prefix : string) padChar = 
        if isGFormat then
            fun (fmt : string) (w : int) v ->
                GenericNumber.leftJustifyWithGFormat (f fmt v) (isNumber v) (isInteger v) (isPositive v) w prefix padChar
        else
            fun (fmt : string) (w : int) v ->
                GenericNumber.leftJustifyWithNonGFormat (f fmt v) (isNumber v) (isPositive v) w prefix padChar  

    let inline rightJustify f (prefixForPositives : string) padChar =
        if padChar = '0' then
            fun (fmt : string) (w : int) v ->
                GenericNumber.rightJustifyWithZeroAsPadChar (f fmt v) (isNumber v) (isPositive v) w prefixForPositives
        else
            System.Diagnostics.Debug.Assert((padChar = ' '))
            fun (fmt : string) (w : int) v ->
                GenericNumber.rightJustifyWithSpaceAsPadChar (f fmt v) (isNumber v) (isPositive v) w prefixForPositives

let isDecimalFormatSpecifier (spec : FormatSpecifier) = 
    spec.TypeChar = 'M'

let getPadAndPrefix allowZeroPadding (spec : FormatSpecifier) = 
    let padChar = if allowZeroPadding && isPadWithZeros spec.Flags then '0' else ' ';
    let prefix = 
        if isPlusForPositives spec.Flags then "+" 
        elif isSpaceForPositives spec.Flags then " "
        else ""
    padChar, prefix    

let isGFormat(spec : FormatSpecifier) = 
    isDecimalFormatSpecifier spec || System.Char.ToLower(spec.TypeChar) = 'g'

let inline basicWithPadding (spec : FormatSpecifier) f =
    let padChar, _ = getPadAndPrefix false spec
    Padding.withPadding spec f (Basic.leftJustify f padChar) (Basic.rightJustify f padChar)
    
let inline numWithPadding (spec : FormatSpecifier) isUnsigned f  =
    let allowZeroPadding = not (isLeftJustify spec.Flags) || isDecimalFormatSpecifier spec
    let padChar, prefix = getPadAndPrefix allowZeroPadding spec
    let isGFormat = isGFormat spec
    Padding.withPadding spec (GenericNumber.noJustification f prefix isUnsigned) (GenericNumber.leftJustify isGFormat f prefix padChar isUnsigned) (GenericNumber.rightJustify f prefix padChar isUnsigned)

let inline decimalWithPadding (spec : FormatSpecifier) getFormat defaultFormat f =
    let padChar, prefix = getPadAndPrefix true spec
    let isGFormat = isGFormat spec
    Padding.withPaddingFormatted spec getFormat defaultFormat (GenericNumber.noJustificationWithFormat f prefix) (GenericNumber.leftJustifyWithFormat isGFormat f prefix padChar) (GenericNumber.rightJustifyWithFormat f prefix padChar)

let inline floatWithPadding (spec : FormatSpecifier) getFormat defaultFormat f =
    let padChar, prefix = getPadAndPrefix true spec
    let isGFormat = isGFormat spec
    Padding.withPaddingFormatted spec getFormat defaultFormat (Float.noJustification f prefix) (Float.leftJustify isGFormat f prefix padChar) (Float.rightJustify f prefix padChar)

let inline identity v =  v
let inline toString  v =   (^T : (member ToString : IFormatProvider -> string)(v, System.Globalization.CultureInfo.InvariantCulture))
let inline toFormattedString fmt = fun (v : ^T) -> (^T : (member ToString : string * IFormatProvider -> string)(v, fmt, System.Globalization.CultureInfo.InvariantCulture))

let inline numberToString c spec alt unsignedConv  =
    if c = 'd' || c = 'i' then
        numWithPadding spec false (alt >> toString : ^T -> string)
    elif c = 'u' then
        numWithPadding spec true  (alt >> unsignedConv >> toString : ^T -> string) 
    elif c = 'x' then
        numWithPadding spec true (alt >> toFormattedString "x" : ^T -> string)
    elif c = 'X' then
        numWithPadding spec true (alt >> toFormattedString "X" : ^T -> string )
    elif c = 'o' then
        numWithPadding spec true (fun (v : ^T) -> Convert.ToString(int64(unsignedConv (alt v)), 8))
    else raise (ArgumentException())    

type ObjectPrinter = 
    static member ObjectToString<'T>(spec : FormatSpecifier) = 
        basicWithPadding spec (fun (v : 'T) -> match box v with null -> "<null>" | x -> x.ToString())
        
    static member GenericToStringCore(v : 'T, opts : FormatOptions, bindingFlags) = 
        // printfn %0A is considered to mean 'print width zero'
        match box v with 
        | null -> "<null>" 
        | _ ->
            failwith "%A not supported"
            //Microsoft.FSharp.Text.StructuredPrintfImpl.Display.anyToStringForPrintf opts bindingFlags v

    static member GenericToString<'T>(spec : FormatSpecifier) = 
        let bindingFlags = 
#if FX_RESHAPED_REFLECTION
            isPlusForPositives spec.Flags // true - show non-public
#else
            if isPlusForPositives spec.Flags then BindingFlags.Public ||| BindingFlags.NonPublic
            else BindingFlags.Public 
#endif

        let useZeroWidth = isPadWithZeros spec.Flags
        let opts = 
            let o = FormatOptions.Default
            let o =
                if useZeroWidth then { o with PrintWidth = 0} 
                elif spec.IsWidthSpecified then { o with PrintWidth = spec.Width}
                else o
            if spec.IsPrecisionSpecified then { o with PrintSize = spec.Precision}
            else o
        match spec.IsStarWidth, spec.IsStarPrecision with
        | true, true ->
            box (fun (v : 'T) (width : int) (prec : int) ->
                let opts = { opts with PrintSize = prec }
                let opts  = if not useZeroWidth then { opts with PrintWidth = width} else opts
                ObjectPrinter.GenericToStringCore(v, opts, bindingFlags)
                )
        | true, false ->
            box (fun (v : 'T) (width : int) ->
                let opts  = if not useZeroWidth then { opts with PrintWidth = width} else opts
                ObjectPrinter.GenericToStringCore(v, opts, bindingFlags)
                )
        | false, true ->
            box (fun (v : 'T) (prec : int) ->
                let opts = { opts with PrintSize = prec }
                ObjectPrinter.GenericToStringCore(v, opts, bindingFlags)
                )
        | false, false ->
            box (fun (v : 'T) ->
                ObjectPrinter.GenericToStringCore(v, opts, bindingFlags)
                )
    
let basicNumberToString (ty : Type) (spec : FormatSpecifier) =
    System.Diagnostics.Debug.Assert(not spec.IsPrecisionSpecified, "not spec.IsPrecisionSpecified")

    let ch = spec.TypeChar

    match Type.GetTypeCode(ty) with
    | TypeCode.Int32    -> numberToString ch spec identity (uint32 : int -> uint32) 
    | TypeCode.Int64    -> numberToString ch spec identity (uint64 : int64 -> uint64)
    | TypeCode.Byte     -> numberToString ch spec identity (byte : byte -> byte) 
    | TypeCode.SByte    -> numberToString ch spec identity (byte : sbyte -> byte)
    | TypeCode.Int16    -> numberToString ch spec identity (uint16 : int16 -> uint16)
    | TypeCode.UInt16   -> numberToString ch spec identity (uint16 : uint16 -> uint16)
    | TypeCode.UInt32   -> numberToString ch spec identity (uint32 : uint32 -> uint32)
    | TypeCode.UInt64   -> numberToString ch spec identity (uint64 : uint64 -> uint64)
    | _ ->
    if ty === typeof<nativeint> then 
        if IntPtr.Size = 4 then 
            numberToString ch spec (fun (v : IntPtr) -> v.ToInt32()) uint32
        else
            numberToString ch spec (fun (v : IntPtr) -> v.ToInt64()) uint64
    elif ty === typeof<unativeint> then 
        if IntPtr.Size = 4 then
            numberToString ch spec (fun (v : UIntPtr) -> v.ToUInt32()) uint32
        else
            numberToString ch spec (fun (v : UIntPtr) -> v.ToUInt64()) uint64

    else raise (ArgumentException(ty.Name + " not a basic integer type"))

let basicFloatToString ty spec = 
    let defaultFormat = getFormatForFloat spec.TypeChar DefaultPrecision
    match Type.GetTypeCode(ty) with
    | TypeCode.Single   -> floatWithPadding spec (getFormatForFloat spec.TypeChar) defaultFormat (fun fmt (v : float32) -> toFormattedString fmt v)
    | TypeCode.Double   -> floatWithPadding spec (getFormatForFloat spec.TypeChar) defaultFormat (fun fmt (v : float) -> toFormattedString fmt v)
    | TypeCode.Decimal  -> decimalWithPadding spec (getFormatForFloat spec.TypeChar) defaultFormat (fun fmt (v : decimal) -> toFormattedString fmt v)
    | _ -> raise (ArgumentException(ty.Name + " not a basic floating point type"))

let getValueConverter (ty : Type) (spec : FormatSpecifier) : obj = 
    match spec.TypeChar with
    | 'b' ->  
        System.Diagnostics.Debug.Assert(ty === typeof<bool>, "ty === typeof<bool>")
        basicWithPadding spec boolToString
    | 's' ->
        System.Diagnostics.Debug.Assert(ty === typeof<string>, "ty === typeof<string>")
        basicWithPadding spec stringToSafeString
    | 'c' ->
        System.Diagnostics.Debug.Assert(ty === typeof<char>, "ty === typeof<char>")
        basicWithPadding spec (fun (c : char) -> c.ToString())
    | 'M'  ->
        System.Diagnostics.Debug.Assert(ty === typeof<decimal>, "ty === typeof<decimal>")
        decimalWithPadding spec (fun _ -> "G") "G" (fun fmt (v : decimal) -> toFormattedString fmt v) // %M ignores precision
    | 'd' | 'i' | 'x' | 'X' | 'u' | 'o'-> 
        basicNumberToString ty spec
    | 'e' | 'E' 
    | 'f' | 'F' 
    | 'g' | 'G' -> 
        basicFloatToString ty spec
    | 'A' ->
        let mi = typeof<ObjectPrinter>.GetMethod("GenericToString", NonPublicStatics)
        let mi = mi.MakeGenericMethod(ty)
        mi.Invoke(null, [| box spec |])
    | 'O' -> 
        let mi = typeof<ObjectPrinter>.GetMethod("ObjectToString", NonPublicStatics)
        let mi = mi.MakeGenericMethod(ty)
        mi.Invoke(null, [| box spec |])
    | _ -> 
        raise (ArgumentException("Bad format specifier"))
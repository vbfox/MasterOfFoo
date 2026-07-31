// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

namespace BlackFox.MasterOfFoo.Core

open System
open System.Reflection
open System.Globalization

/// These are a typical set of options used to control structured formatting.
[<NoEquality; NoComparison>]
type internal FormatOptions =
    { FloatingPointFormat: string
      AttributeProcessor: (string -> (string * string) list -> bool -> unit)
      FormatProvider: IFormatProvider
      BindingFlags: BindingFlags
      PrintWidth: int
      PrintDepth: int
      PrintLength: int
      PrintSize: int
      ShowProperties: bool
      ShowIEnumerable: bool
    }

    static member Default =
        { FormatProvider = (CultureInfo.InvariantCulture :> IFormatProvider)
          AttributeProcessor= (fun _ _ _ -> ())
          BindingFlags = BindingFlags.Public
          FloatingPointFormat = "g10"
          PrintWidth = 80
          PrintDepth = 100
          PrintLength = 100
          PrintSize = 10000
          ShowProperties = false
          ShowIEnumerable = true
        }

module internal Display =
    let anyToStringForPrintf (_options: FormatOptions) (_bindingFlags:BindingFlags) (value, _typValue: Type): string =
        sprintf "%A" value

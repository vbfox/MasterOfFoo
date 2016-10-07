namespace BlackFox.MasterOfFoo

open System

/// <summary>
/// Abstracts generated printer from the details of particular environment: how to write text, how to produce results etc...
/// </summary>
/// <typeparam name="'State">The state of the printer, passed as argument when using '%t'.</typeparam>
/// <typeparam name="'Residue">The type that methods passed to '%t' must return.</typeparam>
/// <typeparam name="'Result">The final result type for the printer.</typeparam>
[<AbstractClass>]
type PrintfEnv<'State, 'Residue, 'Result> =
    val State : 'State
    new(s : 'State) = { State = s }
    
    /// Create the final result for this printer
    abstract Finalize : unit -> 'Result
    
    /// Write an element from the format string (Raw text or format specifier) to the printer
    abstract Write : PrintableElement -> unit
    
    /// Write the result of the method provided by %t to the printer.
    abstract WriteT : 'Residue -> unit
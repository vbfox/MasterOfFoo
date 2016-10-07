namespace BlackFox.MasterOfFoo

open System

/// Abstracts generated printer from the details of particular environment: how to write text, how to produce results etc...
[<AbstractClass>]
type PrintfEnv<'State, 'Residue, 'Result> =
    val State : 'State
    new(s : 'State) = { State = s }
    abstract Finalize : unit -> 'Result
    abstract Write : PrintableElement -> unit
    abstract WriteT : 'Residue -> unit
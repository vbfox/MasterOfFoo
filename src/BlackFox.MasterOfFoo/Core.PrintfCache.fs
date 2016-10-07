module internal BlackFox.MasterOfFoo.Core.PrintfCache

open BlackFox.MasterOfFoo.Core.FormatToString
open BlackFox.MasterOfFoo.Core.PrintfBuilding
open System
open System.Collections.Generic

/// Type of element that is stored in cache 
/// Pair: factory for the printer + number of text blocks that printer will produce (used to preallocate buffers)
type CachedItem<'T, 'State, 'Residue, 'Result> = PrintfFactory<'State, 'Residue, 'Result, 'T> * int

/// 2-level cache.
/// 1st-level stores last value that was consumed by the current thread in thread-static field thus providing shortcuts for scenarios when 
/// printf is called in tight loop
/// 2nd level is global dictionary that maps format string to the corresponding PrintfFactory
type Cache<'T, 'State, 'Residue, 'Result>() =
    static let generate(fmt) = PrintfBuilder<'State, 'Residue, 'Result>().Build<'T>(fmt)        
    static let mutable map = System.Collections.Concurrent.ConcurrentDictionary<string, CachedItem<'T, 'State, 'Residue, 'Result>>()
    static let getOrAddFunc = Func<_, _>(generate)

    static member inline Get(key : string) = map.GetOrAdd(key, getOrAddFunc)

    [<DefaultValue>]
    [<ThreadStatic>]
    static val mutable private last : string * CachedItem<'T, 'State, 'Residue, 'Result>
    
    static member inline Get(key : Format<'T, 'State, 'Residue, 'Result>) =
        if not (Cache<'T, 'State, 'Residue, 'Result>.last === null) 
            && key.Value.Equals (fst Cache<'T, 'State, 'Residue, 'Result>.last) then
                snd Cache<'T, 'State, 'Residue, 'Result>.last
        else
            let v = Cache<'T, 'State, 'Residue, 'Result>.Get(key.Value)
            Cache<'T, 'State, 'Residue, 'Result>.last <- (key.Value, v)
            v
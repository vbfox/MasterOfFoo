namespace BlackFox.MasterOfFoo

open System.Runtime.CompilerServices

#if NET40

[<Extension>]
type ReflectionCompat =
    [<Extension>]
    static member inline GetTypeInfo(t: System.Type) =
        t

#endif

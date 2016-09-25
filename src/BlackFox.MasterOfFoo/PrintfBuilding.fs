// Basic idea of implementation:
// Every Printf.* family should returns curried function that collects arguments and then somehow prints them.
// Idea - instead of building functions on fly argument by argument we instead introduce some predefined parts and then construct functions from these parts
// Parts include:
// Plain ones:
// 1. Final pieces (1..5) - set of functions with arguments number 1..5. 
// Primary characteristic - these functions produce final result of the *printf* operation
// 2. Chained pieces (1..5) - set of functions with arguments number 1..5. 
// Primary characteristic - these functions doesn not produce final result by itself, instead they tailed with some another piece (chained or final).
// Plain parts correspond to simple format specifiers (that are projected to just one parameter of the function, say %d or %s). However we also have 
// format specifiers that can be projected to more than one argument (i.e %a, %t or any simple format specified with * width or precision). 
// For them we add special cases (both chained and final to denote that they can either return value themselves or continue with some other piece)
// These primitives allow us to construct curried functions with arbitrary signatures.
// For example: 
// - function that corresponds to %s%s%s%s%s (string -> string -> string -> string -> string -> T) will be represented by one piece final 5.
// - function that has more that 5 arguments will include chained parts: %s%s%s%s%s%d%s  => chained2 -> final 5
// Primary benefits: 
// 1. creating specialized version of any part requires only one reflection call. This means that we can handle up to 5 simple format specifiers
// with just one reflection call
// 2. we can make combinable parts independent from particular printf implementation. Thus final result can be cached and shared. 
// i.e when first call to printf "%s %s" will trigger creation of the specialization. Subsequent calls will pick existing specialization

module MasterOfFoo.Core.PrintfBuilding

open MasterOfFoo.Core.FormatSpecification
open MasterOfFoo.Core.FormatToString
open System
open System.Collections.Generic
open System.Reflection

type Utils =
    static member inline Write (env : PrintfEnv<_, _, _>, a, b) =
        env.Write a
        env.Write b
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c) =
        Utils.Write(env, a, b)
        env.Write c
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c, d) =
        Utils.Write(env, a, b)
        Utils.Write(env, c, d)
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c, d, e) =
        Utils.Write(env, a, b, c)
        Utils.Write(env, d, e)
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c, d, e, f) =
        Utils.Write(env, a, b, c, d)
        Utils.Write(env, e, f)
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c, d, e, f, g) =
        Utils.Write(env, a, b, c, d, e)
        Utils.Write(env, f, g)
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c, d, e, f, g, h) =
        Utils.Write(env, a, b, c, d, e, f)
        Utils.Write(env, g, h)
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c, d, e, f, g, h, i) =
        Utils.Write(env, a, b, c, d, e, f, g)
        Utils.Write(env, h ,i)
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c, d, e, f, g, h, i, j) =
        Utils.Write(env, a, b, c, d, e, f, g, h)
        Utils.Write(env, i, j)
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c, d, e, f, g, h, i, j, k) =
        Utils.Write(env, a, b, c, d, e, f, g, h, i)
        Utils.Write(env, j, k)
    static member inline Write (env : PrintfEnv<_, _, _>, a, b, c, d, e, f, g, h, i, j, k, l, m) =
        Utils.Write(env, a, b, c, d, e, f, g, h, i, j, k)
        Utils.Write(env, l, m)
    
/// Type of results produced by specialization
/// This is function that accepts thunk to create PrintfEnv on demand and returns concrete instance of Printer (curried function)
/// After all arguments is collected, specialization obtains concrete PrintfEnv from the thunk and use it to output collected data.
type PrintfFactory<'State, 'Residue, 'Result, 'Printer> = (unit -> PrintfEnv<'State, 'Residue, 'Result>) -> 'Printer

[<Literal>]
let MaxArgumentsInSpecialization = 5

/// Specializations are created via factory methods. These methods accepts 2 kinds of arguments
/// - parts of format string that corresponds to raw text
/// - functions that can transform collected values to strings
/// basic shape of the signature of specialization
/// <prefix-string> + <converter for arg1> + <suffix that comes after arg1> + ... <converter for arg-N> + <suffix that comes after arg-N>
type Specializations<'State, 'Residue, 'Result> private ()=
     
    static member Final1<'A>
        (
            s0, conv1, s1
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) ->
                let env = env()
                Utils.Write(env, s0, (conv1 a), s1)
                env.Finalize()
            )
        )
    static member Final2<'A, 'B>
        (
            s0, conv1, s1, conv2, s2
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) (b : 'B) ->
                let env = env()
                Utils.Write(env, s0, (conv1 a), s1, (conv2 b), s2)
                env.Finalize()
            )
        )

    static member Final3<'A, 'B, 'C>
        (
            s0, conv1, s1, conv2, s2, conv3, s3
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) (b : 'B) (c : 'C) ->
                let env = env()
                Utils.Write(env, s0, (conv1 a), s1, (conv2 b), s2, (conv3 c), s3)
                env.Finalize()
            )
        )

    static member Final4<'A, 'B, 'C, 'D>
        (
            s0, conv1, s1, conv2, s2, conv3, s3, conv4, s4
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) (b : 'B) (c : 'C) (d : 'D)->
                let env = env()
                Utils.Write(env, s0, (conv1 a), s1, (conv2 b), s2, (conv3 c), s3, (conv4 d), s4)
                env.Finalize()
            )
        )
    static member Final5<'A, 'B, 'C, 'D, 'E>
        (
            s0, conv1, s1, conv2, s2, conv3, s3, conv4, s4, conv5, s5
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) (b : 'B) (c : 'C) (d : 'D) (e : 'E)->
                let env = env()
                Utils.Write(env, s0, (conv1 a), s1, (conv2 b), s2, (conv3 c), s3, (conv4 d), s4, (conv5 e), s5)
                env.Finalize()
            )
        )
    static member Chained1<'A, 'Tail>
        (
            s0, conv1,
            next
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) ->
                let env() = 
                    let env = env()
                    Utils.Write(env, s0, (conv1 a))
                    env
                next env : 'Tail
            )
        )
    static member Chained2<'A, 'B, 'Tail>
        (
            s0, conv1, s1, conv2,
            next
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) (b : 'B) ->
                let env() = 
                    let env = env()
                    Utils.Write(env, s0, (conv1 a), s1, (conv2 b))
                    env
                next env : 'Tail
            )
        )

    static member Chained3<'A, 'B, 'C, 'Tail>
        (
            s0, conv1, s1, conv2, s2, conv3,
            next
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) (b : 'B) (c : 'C) ->
                let env() = 
                    let env = env()
                    Utils.Write(env, s0, (conv1 a), s1, (conv2 b), s2, (conv3 c))
                    env
                next env : 'Tail
            )
        )

    static member Chained4<'A, 'B, 'C, 'D, 'Tail>
        (
            s0, conv1, s1, conv2, s2, conv3, s3, conv4,
            next
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) (b : 'B) (c : 'C) (d : 'D)->
                let env() = 
                    let env = env()
                    Utils.Write(env, s0, (conv1 a), s1, (conv2 b), s2, (conv3 c), s3, (conv4 d))
                    env
                next env : 'Tail
            )
        )
    static member Chained5<'A, 'B, 'C, 'D, 'E, 'Tail>
        (
            s0, conv1, s1, conv2, s2, conv3, s3, conv4, s4, conv5,
            next
        ) =
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (a : 'A) (b : 'B) (c : 'C) (d : 'D) (e : 'E)->
                let env() = 
                    let env = env()
                    Utils.Write(env, s0, (conv1 a), s1, (conv2 b), s2, (conv3 c), s3, (conv4 d), s4, (conv5 e))
                    env
                next env : 'Tail
            )
        )

    static member TFinal(s1 : PrintableElement, s2 : PrintableElement) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (f : 'State -> 'Residue) -> 
                let env = env()
                env.Write(s1)
                env.WriteT(f env.State)
                env.Write s2
                env.Finalize()
            )
        )
    static member TChained<'Tail>(s1 : PrintableElement, next : PrintfFactory<'State, 'Residue, 'Result,'Tail>) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (f : 'State -> 'Residue) -> 
                let env() = 
                    let env = env()
                    env.Write(s1)
                    env.WriteT(f env.State)
                    env
                next(env) : 'Tail
            )
        )

    static member LittleAFinal<'A>(s1 : PrintableElement, s2 : PrintableElement) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (f : 'State -> 'A ->'Residue) (a : 'A) -> 
                let env = env()
                env.Write s1
                env.WriteT(f env.State a)
                env.Write s2
                env.Finalize()
            )
        )
    static member LittleAChained<'A, 'Tail>(s1 : PrintableElement, next : PrintfFactory<'State, 'Residue, 'Result,'Tail>) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (f : 'State -> 'A ->'Residue) (a : 'A) -> 
                let env() = 
                    let env = env()
                    env.Write s1
                    env.WriteT(f env.State a)
                    env
                next env : 'Tail
            )
        )

    static member StarFinal1<'A>(s1 : PrintableElement, conv, s2 : PrintableElement) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (star1 : int) (a : 'A) -> 
                let env = env()
                env.Write s1
                env.Write (conv a star1 : PrintableElement)
                env.Write s2
                env.Finalize()
            )
        )
        
    static member PercentStarFinal1(s1 : PrintableElement, s2 : PrintableElement) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (_star1 : int) -> 
                let env = env()
                env.Write s1
                env.Write(PrintableElement.MadeByEngine("%"))
                env.Write s2
                env.Finalize()
            )
        )

    static member StarFinal2<'A>(s1 : PrintableElement, conv, s2 : PrintableElement) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (star1 : int) (star2 : int) (a : 'A) -> 
                let env = env()
                env.Write s1
                env.Write (conv a star1 star2: PrintableElement)
                env.Write s2
                env.Finalize()
            )
        )

    /// Handles case when '%*.*%' is used at the end of string
    static member PercentStarFinal2(s1 : PrintableElement, s2 : PrintableElement) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (_star1 : int) (_star2 : int) -> 
                let env = env()
                env.Write s1
                env.Write(PrintableElement.MadeByEngine("%"))
                env.Write s2
                env.Finalize()
            )
        )

    static member StarChained1<'A, 'Tail>(s1 : PrintableElement, conv, next : PrintfFactory<'State, 'Residue, 'Result,'Tail>) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (star1 : int) (a : 'A) -> 
                let env() =
                    let env = env()
                    env.Write s1
                    env.Write(conv a star1 : PrintableElement)
                    env
                next env : 'Tail
            )
        )
        
    /// Handles case when '%*%' is used in the middle of the string so it needs to be chained to another printing block
    static member PercentStarChained1<'Tail>(s1 : PrintableElement, next : PrintfFactory<'State, 'Residue, 'Result,'Tail>) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (_star1 : int) -> 
                let env() =
                    let env = env()
                    env.Write s1
                    env.Write(PrintableElement.MadeByEngine("%"))
                    env
                next env : 'Tail
            )
        )

    static member StarChained2<'A, 'Tail>(s1 : PrintableElement, conv, next : PrintfFactory<'State, 'Residue, 'Result,'Tail>) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (star1 : int) (star2 : int) (a : 'A) -> 
                let env() =
                    let env = env()
                    env.Write s1
                    env.Write(conv a star1 star2 : PrintableElement)
                    env
                next env : 'Tail
            )
        )
        
    /// Handles case when '%*.*%' is used in the middle of the string so it needs to be chained to another printing block
    static member PercentStarChained2<'Tail>(s1 : PrintableElement, next : PrintfFactory<'State, 'Residue, 'Result,'Tail>) = 
        (fun (env : unit -> PrintfEnv<'State, 'Residue, 'Result>) ->
            (fun (_star1 : int) (_star2 : int) -> 
                let env() =
                    let env = env()
                    env.Write s1
                    env.Write(PrintableElement.MadeByEngine("%"))
                    env
                next env : 'Tail
            )
        )
    
    
   
    
let extractCurriedArguments (ty : Type) n = 
    System.Diagnostics.Debug.Assert(n = 1 || n = 2 || n = 3, "n = 1 || n = 2 || n = 3")
    let buf = Array.zeroCreate (n + 1)
    let rec go (ty : Type) i = 
        if i < n then
            match ty.GetGenericArguments() with
            | [| argTy; retTy|] ->
                buf.[i] <- argTy
                go retTy (i + 1)
            | _ -> failwith (String.Format("Expected function with {0} arguments", n))
        else 
            System.Diagnostics.Debug.Assert((i = n), "i = n")
            buf.[i] <- ty
            buf           
    go ty 0
    
[<Literal>]
let ContinuationOnStack = -1
    
type private PrintfBuilderStack() = 
    let args = Stack(10)
    let types = Stack(5)

    let stackToArray size start count (s : Stack<_>) = 
        let arr = Array.zeroCreate size
        for i = 0 to count - 1 do
            arr.[start + i] <- s.Pop()
        arr
        
    member this.GetArgumentAndTypesAsArrays
        (
            argsArraySize, argsArrayStartPos, argsArrayTotalCount, 
            typesArraySize, typesArrayStartPos, typesArrayTotalCount 
        ) = 
        let argsArray = stackToArray argsArraySize argsArrayStartPos argsArrayTotalCount args
        let typesArray = stackToArray typesArraySize typesArrayStartPos typesArrayTotalCount types
        argsArray, typesArray

    member this.PopContinuationWithType() = 
        System.Diagnostics.Debug.Assert(args.Count = 1, "args.Count = 1")
        System.Diagnostics.Debug.Assert(types.Count = 1, "types.Count = 1")
            
        let cont = args.Pop()
        let contTy = types.Pop()

        cont, contTy

    member this.PopValueUnsafe() = args.Pop()

    member this.PushContinuationWithType (cont : obj, contTy : Type) = 
        System.Diagnostics.Debug.Assert(this.IsEmpty, "this.IsEmpty")
        System.Diagnostics.Debug.Assert(
            (
                let _arg, retTy = Microsoft.FSharp.Reflection.FSharpType.GetFunctionElements(cont.GetType())
                contTy.IsAssignableFrom retTy
            ),
            "incorrect type"
            )

        this.PushArgumentWithType(cont, contTy)

    member this.PushArgument(value : obj) =
        args.Push value

    member this.PushArgumentWithType(value : obj, ty) =
        args.Push value
        types.Push ty

    member this.HasContinuationOnStack(expectedNumberOfArguments) = 
        types.Count = expectedNumberOfArguments + 1

    member this.IsEmpty = 
        System.Diagnostics.Debug.Assert(args.Count = types.Count, "args.Count = types.Count")
        args.Count = 0

/// Parses format string and creates result printer function.
/// First it recursively consumes format string up to the end, then during unwinding builds printer using PrintfBuilderStack as storage for arguments.
/// idea of implementation is very simple: every step can either push argument to the stack (if current block of 5 format specifiers is not yet filled) 
//  or grab the content of stack, build intermediate printer and push it back to stack (so it can later be consumed by as argument) 
type PrintfBuilder<'S, 'Re, 'Res>() =
    
    let mutable count = 0
           
    let buildSpecialChained(spec : FormatSpecifier, argTys : Type[], prefix : PrintableElement, tail : obj, retTy) = 
        if spec.TypeChar = 'a' then
            let mi = typeof<Specializations<'S, 'Re, 'Res>>.GetMethod("LittleAChained", NonPublicStatics)
            verifyMethodInfoWasTaken mi
            let mi = mi.MakeGenericMethod([| argTys.[1];  retTy |])
            let args = [| box prefix; tail   |]
            mi.Invoke(null, args)
        elif spec.TypeChar = 't' then
            let mi = typeof<Specializations<'S, 'Re, 'Res>>.GetMethod("TChained", NonPublicStatics)
            verifyMethodInfoWasTaken mi
            let mi = mi.MakeGenericMethod([| retTy |])
            let args = [| box prefix; tail |]
            mi.Invoke(null, args)
        else
            System.Diagnostics.Debug.Assert(spec.IsStarPrecision || spec.IsStarWidth , "spec.IsStarPrecision || spec.IsStarWidth ")

            let mi = 
                let n = if spec.IsStarWidth = spec.IsStarPrecision then 2 else 1
                let prefix = if spec.TypeChar = '%' then "PercentStarChained" else "StarChained"
                let name = prefix + (string n)
                typeof<Specializations<'S, 'Re, 'Res>>.GetMethod(name, NonPublicStatics)
                
            verifyMethodInfoWasTaken mi
                
            let argTypes, args =
                if spec.TypeChar = '%' then
                    [| retTy |], [| box prefix; tail |]
                else
                    let argTy = argTys.[argTys.Length - 2]
                    let conv = getValueConverter argTy spec 
                    [| argTy; retTy |], [| box prefix; box conv; tail |]
                
            let mi = mi.MakeGenericMethod argTypes
            mi.Invoke(null, args)
            
    let buildSpecialFinal(spec : FormatSpecifier, argTys : Type[], prefix : PrintableElement, suffix : PrintableElement) =
        if spec.TypeChar = 'a' then
            let mi = typeof<Specializations<'S, 'Re, 'Res>>.GetMethod("LittleAFinal", NonPublicStatics)
            verifyMethodInfoWasTaken mi
            let mi = mi.MakeGenericMethod(argTys.[1] : Type)
            let args = [| box prefix; box suffix |]
            mi.Invoke(null, args)
        elif spec.TypeChar = 't' then
            let mi = typeof<Specializations<'S, 'Re, 'Res>>.GetMethod("TFinal", NonPublicStatics)
            verifyMethodInfoWasTaken mi
            let args = [| box prefix; box suffix |]
            mi.Invoke(null, args)
        else
            System.Diagnostics.Debug.Assert(spec.IsStarPrecision || spec.IsStarWidth , "spec.IsStarPrecision || spec.IsStarWidth ")

            let mi = 
                let n = if spec.IsStarWidth = spec.IsStarPrecision then 2 else 1
                let prefix = if spec.TypeChar = '%' then "PercentStarFinal" else "StarFinal"
                let name = prefix + (string n)
                typeof<Specializations<'S, 'Re, 'Res>>.GetMethod(name, NonPublicStatics)
               
            verifyMethodInfoWasTaken mi

            let mi', args = 
                if spec.TypeChar = '%' then 
                    mi, [| box prefix; box suffix  |]
                else
                    let argTy = argTys.[argTys.Length - 2]
                    let mi = mi.MakeGenericMethod(argTy)
                    let conv = getValueConverter argTy spec 
                    mi, [| box prefix; box conv; box suffix  |]

            mi'.Invoke(null, args)

    let buildPlainFinal(args : obj[], argTypes : Type[]) = 
        let methodName = "Final" + (argTypes.Length.ToString())
        let mi = typeof<Specializations<'S, 'Re, 'Res>>.GetMethod(methodName, NonPublicStatics)
        verifyMethodInfoWasTaken mi
        let mi' = mi.MakeGenericMethod(argTypes)
        mi'.Invoke(null, args)
    
    let buildPlainChained(args : obj[], argTypes : Type[]) = 
        let mi = typeof<Specializations<'S, 'Re, 'Res>>.GetMethod("Chained" + ((argTypes.Length - 1).ToString()), NonPublicStatics)
        verifyMethodInfoWasTaken mi
        let mi' = mi.MakeGenericMethod(argTypes)
        mi'.Invoke(null, args)   

    let builderStack = PrintfBuilderStack()

    let ContinuationOnStack = -1

    let buildPlain numberOfArgs prefix = 
        let n = numberOfArgs * 2
        let hasCont = builderStack.HasContinuationOnStack numberOfArgs

        let extra = if hasCont then 1 else 0
        let plainArgs, plainTypes = 
            builderStack.GetArgumentAndTypesAsArrays(n + 1, 1, n, (numberOfArgs + extra), 0, numberOfArgs)

        plainArgs.[0] <- box prefix

        if hasCont then
            let cont, contTy = builderStack.PopContinuationWithType()
            plainArgs.[plainArgs.Length - 1] <- cont
            plainTypes.[plainTypes.Length - 1] <- contTy

            buildPlainChained(plainArgs, plainTypes)
        else
            buildPlainFinal(plainArgs, plainTypes)

    let rec parseFromFormatSpecifier (prefix : PrintableElement) (s : string) (funcTy : Type) i : int = 
            
        if i >= s.Length then 0
        else
            
        System.Diagnostics.Debug.Assert(s.[i] = '%', "s.[i] = '%'")
        count <- count + 1

        let flags, i = FormatString.parseFlags s (i + 1)
        let width, i = FormatString.parseWidth s i
        let precision, i = FormatString.parsePrecision s i
        let typeChar, i = FormatString.parseTypeChar s i
        let spec = { TypeChar = typeChar; Precision = precision; Flags = flags; Width = width}
            
        let next, suffix = FormatString.findNextFormatSpecifier s i

        let argTys = 
            let n = 
                if spec.TypeChar = 'a' then 2 
                elif spec.IsStarWidth || spec.IsStarPrecision then
                    if spec.IsStarWidth = spec.IsStarPrecision then 3 
                    else 2
                else 1

            let n = if spec.TypeChar = '%' then n - 1 else n
                
            System.Diagnostics.Debug.Assert(n <> 0, "n <> 0")

            extractCurriedArguments funcTy n

        let retTy = argTys.[argTys.Length - 1]

        let numberOfArgs = parseFromFormatSpecifier suffix s retTy next

        if spec.TypeChar = 'a' || spec.TypeChar = 't' || spec.IsStarWidth || spec.IsStarPrecision then
            if numberOfArgs = ContinuationOnStack then

                let cont, contTy = builderStack.PopContinuationWithType()
                let currentCont = buildSpecialChained(spec, argTys, prefix, cont, contTy)
                builderStack.PushContinuationWithType(currentCont, funcTy)

                ContinuationOnStack
            else
                if numberOfArgs = 0 then
                    System.Diagnostics.Debug.Assert(builderStack.IsEmpty, "builderStack.IsEmpty")

                    let currentCont = buildSpecialFinal(spec, argTys, prefix, suffix)
                    builderStack.PushContinuationWithType(currentCont, funcTy)
                    ContinuationOnStack
                else
                        
                        
                    let hasCont = builderStack.HasContinuationOnStack(numberOfArgs)
                        
                    let expectedNumberOfItemsOnStack = numberOfArgs * 2
                    let sizeOfTypesArray = 
                        if hasCont then numberOfArgs + 1
                        else numberOfArgs
                                                
                    let plainArgs, plainTypes = 
                        builderStack.GetArgumentAndTypesAsArrays(expectedNumberOfItemsOnStack + 1, 1, expectedNumberOfItemsOnStack, sizeOfTypesArray, 0, numberOfArgs )

                    plainArgs.[0] <- box suffix

                    let next =
                        if hasCont then
                            let nextCont, nextContTy = builderStack.PopContinuationWithType()
                            plainArgs.[plainArgs.Length - 1] <- nextCont
                            plainTypes.[plainTypes.Length - 1] <- nextContTy
                            buildPlainChained(plainArgs, plainTypes)
                        else
                            buildPlainFinal(plainArgs, plainTypes)
                            
                    let next = buildSpecialChained(spec, argTys, prefix, next, retTy)
                    builderStack.PushContinuationWithType(next, funcTy)

                    ContinuationOnStack
        else
            if numberOfArgs = ContinuationOnStack then
                let idx = argTys.Length - 2
                builderStack.PushArgument suffix
                builderStack.PushArgumentWithType((getValueConverter argTys.[idx] spec), argTys.[idx])
                1
            else
                builderStack.PushArgument suffix
                builderStack.PushArgumentWithType((getValueConverter argTys.[0] spec), argTys.[0])
                    
                if numberOfArgs = MaxArgumentsInSpecialization - 1 then
                    let cont = buildPlain (numberOfArgs + 1) prefix
                    builderStack.PushContinuationWithType(cont, funcTy)
                    ContinuationOnStack
                else 
                    numberOfArgs + 1

    let parseFormatString (s : string) (funcTy : System.Type) : obj = 
        let prefixPos, prefix = FormatString.findNextFormatSpecifier s 0
        if prefixPos = s.Length then 
            box (fun (env : unit -> PrintfEnv<'S, 'Re, 'Res>) -> 
                let env = env()
                env.Write prefix
                env.Finalize()
                )
        else
            let n = parseFromFormatSpecifier prefix s funcTy prefixPos
                
            if n = ContinuationOnStack || n = 0 then
                builderStack.PopValueUnsafe()
            else
                buildPlain n prefix
                            
    member this.Build<'T>(s : string) : PrintfFactory<'S, 'Re, 'Res, 'T> * int = 
        parseFormatString s typeof<'T> :?> _, (2 * count + 1) // second component is used in SprintfEnv as value for internal buffer
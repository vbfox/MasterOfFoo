namespace BlackFox.MasterOfFoo

open System

[<NoComparison; NoEquality>]
/// Represents one step in the execution of a format string
type Step =
    | StepWithArg of prefix: PrintableElement * conv: (obj -> PrintableElement)
    | StepWithTypedArg of prefix: PrintableElement * conv: (obj -> Type -> PrintableElement)
    | StepString of prefix: PrintableElement
    | StepLittleT of prefix: PrintableElement
    | StepLittleA of prefix: PrintableElement
    | StepStar1 of prefix: PrintableElement * conv: (obj -> int -> PrintableElement)
    | StepPercentStar1 of prefix: PrintableElement
    | StepStar2 of prefix: PrintableElement * conv: (obj -> int -> int -> PrintableElement)
    | StepPercentStar2 of prefix: PrintableElement

    // Count the number of string fragments in a sequence of steps
    static member BlockCount(steps: Step[]) =
        let mutable count = 0
        for step in steps do
            match step with
            | StepWithArg (prefix, _conv) ->
                if not (prefix.IsNullOrEmpty) then count <- count + 1
                count <- count + 1
            | StepWithTypedArg (prefix, _conv) ->
                if not (prefix.IsNullOrEmpty) then count <- count + 1
                count <- count + 1
            | StepString prefix ->
                if not (prefix.IsNullOrEmpty) then count <- count + 1
            | StepLittleT(prefix) ->
                if not (prefix.IsNullOrEmpty) then count <- count + 1
                count <- count + 1
            | StepLittleA(prefix) ->
                if not (prefix.IsNullOrEmpty) then count <- count + 1
                count <- count + 1
            | StepStar1(prefix, _conv) ->
                if not (prefix.IsNullOrEmpty) then count <- count + 1
                count <- count + 1
            | StepPercentStar1(prefix) ->
                if not (prefix.IsNullOrEmpty) then count <- count + 1
                count <- count + 1
            | StepStar2(prefix, _conv) ->
                if not (prefix.IsNullOrEmpty) then count <- count + 1
                count <- count + 1
            | StepPercentStar2(prefix) ->
                if not (prefix.IsNullOrEmpty) then count <- count + 1
                count <- count + 1
        count

/// Abstracts generated printer from the details of particular environment: how to write text, how to produce results etc...
[<AbstractClass>]
type PrintfEnv<'State, 'Residue, 'Result>(state: 'State) =
    member _.State = state

    abstract Finish: unit -> 'Result

    abstract Write: PrintableElement -> unit

    /// Write the result of a '%t' format.  If this is a string it is written. If it is a 'unit' value
    /// the side effect has already happened
    abstract WriteT: 'Residue -> unit

    member env.WriteSkipEmpty(s: PrintableElement) =
        if not (s.IsNullOrEmpty) then
            env.Write s

    member internal env.RunSteps (args: obj[], argTys: Type[], steps: Step[]) =
        let mutable argIndex = 0
        let mutable tyIndex = 0

        for step in steps do
            match step with
            | StepWithArg (prefix, conv) ->
                env.WriteSkipEmpty prefix
                let arg = args.[argIndex]
                argIndex <- argIndex + 1
                env.Write(conv arg)

            | StepWithTypedArg (prefix, conv) ->
                env.WriteSkipEmpty prefix
                let arg = args.[argIndex]
                let argTy = argTys.[tyIndex]
                argIndex <- argIndex + 1
                tyIndex <- tyIndex + 1
                env.Write(conv arg argTy)

            | StepString prefix ->
                env.WriteSkipEmpty prefix

            | StepLittleT(prefix) ->
                env.WriteSkipEmpty prefix
                let farg = args.[argIndex]
                argIndex <- argIndex + 1
                let f = farg :?> ('State -> 'Residue)
                env.WriteT(f env.State)

            | StepLittleA(prefix) ->
                env.WriteSkipEmpty prefix
                let farg = args.[argIndex]
                argIndex <- argIndex + 1
                let arg = args.[argIndex]
                argIndex <- argIndex + 1
                let f = farg :?> ('State -> obj -> 'Residue)
                env.WriteT(f env.State arg)

            | StepStar1(prefix, conv) ->
                env.WriteSkipEmpty prefix
                let star1 = args.[argIndex] :?> int
                argIndex <- argIndex + 1
                let arg1 = args.[argIndex]
                argIndex <- argIndex + 1
                env.Write (conv arg1 star1)

            | StepPercentStar1(prefix) ->
                argIndex <- argIndex + 1
                env.WriteSkipEmpty prefix
                env.Write(PrintableElement("%", PrintableElementType.MadeByEngine))

            | StepStar2(prefix, conv) ->
                env.WriteSkipEmpty prefix
                let star1 = args.[argIndex] :?> int
                argIndex <- argIndex + 1
                let star2 = args.[argIndex] :?> int
                argIndex <- argIndex + 1
                let arg1 = args.[argIndex]
                argIndex <- argIndex + 1
                env.Write (conv arg1 star1 star2)

            | StepPercentStar2(prefix) ->
                env.WriteSkipEmpty prefix
                argIndex <- argIndex + 2
                env.Write(PrintableElement("%", PrintableElementType.MadeByEngine))

        env.Finish()

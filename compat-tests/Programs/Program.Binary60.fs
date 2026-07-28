module Program

[<EntryPoint>]
let main _ =
    Common.runProgram (fun () ->
        Cases.Floor45.run ()
        Cases.Interpolation50.run ()
        Cases.Binary60.run ())

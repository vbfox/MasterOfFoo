module Main

open System.Threading
open System.Globalization

open Expecto

[<EntryPoint>]
let main args =
    // Some tests use the .Net formating formats like $"{System.Math.PI:N3}" and their representation
    // depends on the culture
    Thread.CurrentThread.CurrentCulture <- CultureInfo.InvariantCulture

    let writeResults = TestResults.writeNUnitSummary "TestResults.xml"
    let config = defaultConfig.appendSummaryHandler writeResults
    runTestsInAssembly config args

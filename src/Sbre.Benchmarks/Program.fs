
open System
open System.IO
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running
open Sbre.Benchmarks


[<EntryPoint>]
let main argv =
    match Environment.GetCommandLineArgs() |> Seq.last with
    | "prefixW" -> BenchmarkRunner.Run(typeof<Optimizations.MatchStartOptimization>) |> ignore
    | _ ->
        ()
    0
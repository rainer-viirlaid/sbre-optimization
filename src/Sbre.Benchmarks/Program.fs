
open System
open System.IO
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running
open Sbre.Benchmarks


[<EntryPoint>]
let main argv =
    match Environment.GetCommandLineArgs() |> Seq.last with
    | "prefixTwain" -> BenchmarkRunner.Run(typeof<Optimizations.MatchStartOptimizationTwain>) |> ignore
    | "prefixTamm" -> BenchmarkRunner.Run(typeof<Optimizations.MatchStartOptimizationTammsaare>) |> ignore
    | "prefixEstWiki" -> BenchmarkRunner.Run(typeof<Optimizations.MatchStartOptimizationEstWiki>) |> ignore
    | _ ->
        ()
    0
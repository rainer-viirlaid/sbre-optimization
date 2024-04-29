
module Program

open Sbre
open Sbre.Benchmarks.Optimizations

let tester = MatchCountingCorrectness()
tester.ManualTesting()


// let tammsaare = __SOURCE_DIRECTORY__ + "/data/Tammsaare Kollektsioon.txt" |> System.IO.File.ReadAllText
//
// let regex = Regex(".{2,4}(Karl|Andres|Pearu|Vanapagan)")
// regex.Count(tammsaare)



module Sbre.Benchmarks.Optimizations

open System.Collections.Generic
open System.Globalization
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.Intrinsics
open System.Runtime.Intrinsics.X86
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Columns
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Exporters.Csv
open BenchmarkDotNet.Reports
open Perfolizer.Horology
open Sbre
open Sbre.Benchmarks.Jobs
open Sbre.Benchmarks.OptimizationsComprehensive
open Sbre.Benchmarks.Paper
open Sbre.Optimizations
open System
open Sbre.Pat
open Sbre.Types
open System.Text.Json.Nodes
open System.Buffers


let twain = __SOURCE_DIRECTORY__ + "/data/input-text.txt" |> System.IO.File.ReadAllText
let sherlock = __SOURCE_DIRECTORY__ + "/data/sherlock.txt" |> System.IO.File.ReadAllText
let subtitlesMed = __SOURCE_DIRECTORY__ + "/data/en-medium.txt" |> System.IO.File.ReadAllText
let dotnetRuntime = "C:\Users\Name\Documents\TalTech\Loputoo\.NET Runtime\dotnet.txt" |> System.IO.File.ReadAllText
// let rust = __SOURCE_DIRECTORY__ + "/data/rust-src-tools-3b0d4813.txt" |> System.IO.File.ReadAllText

let testInput =
                twain
                // sherlock |> String.replicate 100
                // "a" |> String.replicate 10_000_000
                // rust
                // subtitlesMed


let twainWeightsJson = __SOURCE_DIRECTORY__ + "/data/charFreqTwain.json"  |> System.IO.File.ReadAllText
let twainWeights100Json = __SOURCE_DIRECTORY__ + "/data/charFreqTwain-100.json"  |> System.IO.File.ReadAllText
let twainWeights1kJson = __SOURCE_DIRECTORY__ + "/data/charFreqTwain-1000.json"  |> System.IO.File.ReadAllText
let twainWeights10kJson = __SOURCE_DIRECTORY__ + "/data/charFreqTwain-10000.json"  |> System.IO.File.ReadAllText
let twainWeights100kJson = __SOURCE_DIRECTORY__ + "/data/charFreqTwain-100000.json"  |> System.IO.File.ReadAllText

let loadJsonCharFrequencies (jsonText: string) =
    let json = JsonValue.Parse jsonText
    (json.Item "characters").AsArray() |> Seq.map (fun charFreq ->
        ((charFreq.Item "character").GetValue<char>(), (charFreq.Item "frequency").GetValue<float>())
        ) |> dict

let twainWeightsFull = loadJsonCharFrequencies twainWeightsJson
let twainWeights100 = loadJsonCharFrequencies twainWeights100Json
let twainWeights1k = loadJsonCharFrequencies twainWeights1kJson
let twainWeights10k = loadJsonCharFrequencies twainWeights10kJson
let twainWeights100k = loadJsonCharFrequencies twainWeights100kJson


module Patterns =
    
    // Patterns from Rebar

    // rust-src-tools-3b0d4813.txt
    // TODO: bitvector error
    // let DATE = System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + "/data/pattern-date.txt" )
    
    // rust-src-tools-3b0d4813.txt
    // let URL = System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + "/data/pattern-url.txt" )
    
    let DICTIONARY_15 = __SOURCE_DIRECTORY__ + "/data/length-15.txt" |> System.IO.File.ReadAllText |> _.Split("\n") |> fun r -> String.Join("|", r)
    
    [<Literal>]
    let SHERLOCK = @"Sherlock Holmes|John Watson|Irene Adler|Inspector Lestrade|Professor Moriarty"
    
    [<Literal>]
    let SHERLOCK_CASEIGNORE = @"(?i)Sherlock Holmes|John Watson|Irene Adler|Inspector Lestrade|Professor Moriarty"

    // Twain patterns from https://zherczeg.github.io/sljit/regex_perf.html

    [<Literal>]
    let TWAIN = @"Twain"

    [<Literal>]
    let TWAIN_CASEIGNORE = @"(?i)Twain"

    [<Literal>]
    let AZ_SHING = @"[a-z]shing"

    [<Literal>]
    let HUCK_SAW = @"Huck[a-zA-Z]+|Saw[a-zA-Z]+"
    
    [<Literal>]
    let WORD_END_ORIGINAL = @"\b\w+nn\b"
    
    [<Literal>]
    let WORD_END = @"\w+nn\W" // Similar to \b\w+nn\b but without anchors(lookarounds) that prevent skipping

    [<Literal>]
    let AQ_X = @"[a-q][^u-z]{13}x"

    [<Literal>]
    let TOM_SAWYER_HUCKLEBERRY_FINN = @"Tom|Sawyer|Huckleberry|Finn"

    [<Literal>]
    let TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE = @"(?i)Tom|Sawyer|Huckleberry|Finn"

    [<Literal>]
    let D02_TOM_SAWYER_HUCKLEBERRY_FINN = @".{0,2}(Tom|Sawyer|Huckleberry|Finn)"

    [<Literal>]
    let D24_TOM_SAWYER_HUCKLEBERRY_FINN = @".{2,4}(Tom|Sawyer|Huckleberry|Finn)"

    [<Literal>]
    let TOM_RIVER = @"Tom.{10,25}river|river.{10,25}Tom"

    [<Literal>]
    let AZ_ING = @"[a-zA-Z]+ing"

    [<Literal>]
    let AZ_ING_SPACES = @"\s[a-zA-Z]{0,12}ing\s"

    [<Literal>]
    let AZ_AWYER_INN = @"([A-Za-z]awyer|[A-Za-z]inn)\s"

    [<Literal>]
    let QUOTES = @"[""'][^""']{0,30}[?!\.][""']"
    
    // Other patterns for Twain

    [<Literal>]
    let HAVE_THERE = @".*have.*&.*there.*"

    [<Literal>]
    let HUCK_AZ = @"Huck[A-Za-z]"

    [<Literal>]
    let AZ_UCK_AZ = @"[A-Za-z]uck[A-Za-z]"

    [<Literal>]
    let H_AZ_CK_AZ = @"H[A-Za-z]ck[A-Za-z]"



type BenchmarkConfig() as self =
    inherit ManualConfig() 
    do
        let summaryStyle = SummaryStyle(CultureInfo.InvariantCulture, true, SizeUnit.B, TimeUnit.Millisecond, false)
                            .WithMaxParameterColumnWidth(60)
        self.SummaryStyle <- summaryStyle
        self
            .AddExporter(CsvExporter(CsvSeparator.Comma, summaryStyle))
            // .With(CsvExporter(CsvSeparator.Comma, summaryStyle))
            |> ignore



[<Config(typedefof<BenchmarkConfig>)>]
[<MemoryDiagnoser(true)>]
[<ShortRunJob>]
type PrefixCharsetSearch () =

    [<Params(
        // Twain regexes
        
        Patterns.TWAIN,
        Patterns.TWAIN_CASEIGNORE,
        Patterns.AZ_SHING,
        Patterns.HUCK_SAW,
        Patterns.WORD_END_ORIGINAL,
        Patterns.WORD_END,
        Patterns.AQ_X,
        Patterns.TOM_SAWYER_HUCKLEBERRY_FINN,
        Patterns.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE,
        Patterns.D02_TOM_SAWYER_HUCKLEBERRY_FINN,
        Patterns.D24_TOM_SAWYER_HUCKLEBERRY_FINN,
        Patterns.TOM_RIVER,
        Patterns.AZ_ING,
        Patterns.AZ_ING_SPACES,
        Patterns.AZ_AWYER_INN,
        Patterns.QUOTES,
        
        Patterns.HAVE_THERE,
        Patterns.HUCK_AZ,
        Patterns.AZ_UCK_AZ,
        Patterns.H_AZ_CK_AZ
        
        // Rebar regexes
        
        // Patterns.SHERLOCK
        // Patterns.SHERLOCK_CASEIGNORE
    )>]
    member val rs: string = "" with get, set
    
    member val regex: Regex = Regex("") with get, set
    
    

    [<GlobalSetup(Target = "Automatic")>]
    member this.AutomaticSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetCharacterWeights(twainWeights100)
    
    [<Benchmark>]
    member this.Automatic() =
        this.regex.Count(testInput)

    // [<GlobalSetup(Target = "Weighted")>]
    // member this.WeightedSetup() =
    //     this.regex <- Regex(this.rs)
    //     this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedExactSets)
    //
    // [<Benchmark>]
    // member this.Weighted() =
    //     this.regex.Count(testInput)
    //
    // [<GlobalSetup(Target = "StringCaseIns")>]
    // member this.StringCaseInsSetup() =
    //     this.regex <- Regex(this.rs)
    //     this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.StringEndCaseIgnore)
    //
    // [<Benchmark>]
    // member this.StringCaseIns() =
    //     this.regex.Count(testInput)
        
    
        
    
    member this.ManualTesting() =
        // this.AlternationSetup()
        // let c = this.Alternation()
        
        // this.regex <- Regex(Patterns.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE)
        // this.regex.TSetMatcher.SetCharacterWeights(twainWeightsFull)
        
        // let a = this.regex.TSetMatcher.SetWeightsFromText(testInput, 11)
        
        // this.MatchCountTesting()
        
        this.regex <- Regex(PatternsTwain.AZ_SHING)
        // this.regex.TSetMatcher.SetCharacterWeights(twainWeights100)
        // this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)
        // this.regex.TSetMatcher.SetWeightsFromText(testInput, 11)
        
        let c = this.regex.Count(twain)
        
        // Console.WriteLine(c |> Seq.length)
        // Console.WriteLine(c)
        ()
    
    member this.MatchCountTesting() =
        let pats = [|
            Patterns.TWAIN // 811
            Patterns.TWAIN_CASEIGNORE // 965
            Patterns.AZ_SHING // 1540
            Patterns.HUCK_SAW // 262
            Patterns.WORD_END // 262
            Patterns.AQ_X // 4094, Rider finds 4081
            Patterns.TOM_SAWYER_HUCKLEBERRY_FINN // 2598
            Patterns.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE // 4152
            Patterns.D02_TOM_SAWYER_HUCKLEBERRY_FINN // 2598
            Patterns.D24_TOM_SAWYER_HUCKLEBERRY_FINN // 1976
            Patterns.TOM_RIVER // 2
            Patterns.AZ_ING // 78 423
            Patterns.AZ_ING_SPACES // 55 248
            Patterns.AZ_AWYER_INN // 209
            Patterns.QUOTES // 8885, Rider finds 8927
           
            Patterns.HAVE_THERE // 426
            Patterns.HUCK_AZ // 56
            Patterns.AZ_UCK_AZ // 706
            Patterns.H_AZ_CK_AZ // 97
        |]
        for pat in pats do
            this.regex <- Regex(pat)
            // this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)
            // this.regex.TSetMatcher.SetCharacterWeights(twainWeightsFull)
            let count = this.regex.Count(twain)
            ()
            
        
   
   
   
[<Config(typedefof<BenchmarkConfig>)>]
[<MemoryDiagnoser(true)>]
[<ShortRunJob>]
type WeightCalculation () =
    
    [<Params(
        100,
        1000,
        10000,
        100000
    )>]
    member val symbolCount: int = 1 with get, set
    
    member val regex: Regex = Regex("") with get, set
        
    [<GlobalSetup(Target = "CalculatingWeights")>]
    member this.CalculatingWeightsSetup() =
        this.regex <- Regex(Patterns.TWAIN)

    [<Benchmark>]
    member this.CalculatingWeights() =
        this.regex.TSetMatcher.SetWeightsFromText(testInput, this.symbolCount)


   
[<Config(typedefof<BenchmarkConfig>)>]
[<MemoryDiagnoser(true)>]
// [<ShortRunJob>]
type WeightsComparison () =

    [<Params(
        // Twain regexes
        
        Patterns.TWAIN,
        Patterns.TWAIN_CASEIGNORE,
        Patterns.AZ_SHING,
        Patterns.HUCK_SAW,
        Patterns.WORD_END,
        Patterns.AQ_X,
        Patterns.TOM_SAWYER_HUCKLEBERRY_FINN,
        Patterns.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE,
        Patterns.D02_TOM_SAWYER_HUCKLEBERRY_FINN,
        Patterns.D24_TOM_SAWYER_HUCKLEBERRY_FINN,
        Patterns.TOM_RIVER,
        Patterns.AZ_ING,
        Patterns.AZ_ING_SPACES,
        Patterns.AZ_AWYER_INN,
        Patterns.QUOTES,
        
        Patterns.HAVE_THERE,
        Patterns.HUCK_AZ,
        Patterns.AZ_UCK_AZ,
        Patterns.H_AZ_CK_AZ
        
        // Rebar regexes
        
        // Patterns.SHERLOCK
        // Patterns.SHERLOCK_CASEIGNORE
    )>]
    member val rs: string = "" with get, set
    
    member val regex: Regex = Regex("") with get, set
    

    [<GlobalSetup(Target = "FullWeights")>]
    member this.FullWeightsSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetCharacterWeights(twainWeightsFull)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)

    [<Benchmark>]
    member this.FullWeights() =
        this.regex.Count(testInput)
    

    [<GlobalSetup(Target = "_100Weights")>]
    member this._100WeightsSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetCharacterWeights(twainWeights100)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)

    [<Benchmark>]
    member this._100Weights() =
        this.regex.Count(testInput)
    

    [<GlobalSetup(Target = "_1kWeights")>]
    member this._1kWeightsSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetCharacterWeights(twainWeights1k)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)

    [<Benchmark>]
    member this._1kWeights() =
        this.regex.Count(testInput)
    

    [<GlobalSetup(Target = "_10kWeights")>]
    member this._10kWeightsSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetCharacterWeights(twainWeights10k)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)

    [<Benchmark>]
    member this._10kWeights() =
        this.regex.Count(testInput)
    

    // [<GlobalSetup(Target = "_100kWeights")>]
    // member this._100kWeightsSetup() =
    //     this.regex <- Regex(this.rs)
    //     this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.AlternationSpecialSet)
    //     this.regex.TSetMatcher.SetCharacterWeights(twainWeights100k)
    //
    // [<Benchmark>]
    // member this._100kWeights() =
    //     this.regex.Count(testInput)




















[<MemoryDiagnoser>]
[<ShortRunJob>]
// [<AbstractClass>]
type StringPrefix(pattern:string) =
    let regex = Sbre.Regex(pattern)
    let matcher = regex.TSetMatcher
    // find optimized prefix for regex
    let getder = (fun (mt,node) ->
        let loc = Pat.Location.getNonInitial()
        matcher.CreateDerivative(&loc, mt,node)
    )

    let charToTSet (chr:char) = matcher.Cache.CharToMinterm(chr)
    // let isElemOfSet (tset1:TSet) (tset2:TSet) = Solver.elemOfSet tset1 tset2

    let svals = [|'n'|].AsMemory()

    // [<Benchmark>]
    // member x.SpanIndexOf() =
    //     // let tc = fullInput.AsSpan().Count("Twain")
    //     let tc = fullInput.AsSpan().Count(")")
    //     ()
        // if tc <> 811 then failwith $"invalid count {tc}"
        // if tc <> 2673 then failwith $"invalid count {tc}"

    [<Benchmark>]
    member x.SpanIndexOf1() =
        let span = testInput.AsSpan()
        let mutable currpos = testInput.Length
        let mutable looping = true
        let mutable tc = 0
        let tlen = "Twain".Length
        while looping do
            // vectorize only to first char
            let slice = span.Slice(0,currpos)
            let newPos = slice.IndexOfAny(svals.Span)
            if newPos = -1 || newPos < tlen then looping <- false else
            currpos <- newPos
            let mstart = currpos - tlen + 1
            let validStart = slice.Slice(mstart).StartsWith("Twain")
            if validStart then
                tc <- tc + 1
                currpos <- mstart
            else currpos <- currpos - 1
        if tc <> 811 then failwith $"invalid count: {tc}"


    // member x.VecLastIndex(vecSpans:ReadOnlySpan<Vector256<uint16>>) =
    //     let enumerator = vecSpans.Slice(0, )
    //     // for (var i = 0; i < vInts.Length; i++)
    //     // {
    //     //     var result = Vector256.Equals(vInts[i], compareValue);
    //     //     if (result == Vector256<int>.Zero) continue;
    //     //
    //     //     for (var k = 0; k < vectorLength; k++)
    //     //         if (result.GetElement(k) != 0)
    //     //             return i * vectorLength + k;
    //     // }

    [<Benchmark>]
    member x.SpanIndexOf2() =
        let origspan = testInput.AsSpan()
        let mutable tc = 0
        let alignAmount = origspan.Length % 16
        let alignSpan = origspan.Slice(alignAmount)
        let inputVectors = MemoryMarshal.Cast<char, Vector256<uint16>>(alignSpan)
        let searchVector = Vector256.Create<uint16>(uint16 'n')
        let onevec = Vector256<uint16>.AllBitsSet
        let idx = inputVectors.Length - 1
        let tlen = "Twain".Length
        let outArray = Array.zeroCreate<uint16> 16
        let outSpan = outArray.AsSpan()

        for i = idx downto 0 do
            let result = Vector256.Equals(inputVectors[i], searchVector)
            if not (Vector256.EqualsAny(result, onevec)) then () else
            Vector256.CopyTo(result,outSpan)
            for j = 0 to 15 do
                if outSpan[j] <> 0us then
                    if j > 0 && inputVectors[i][j-1] <> uint16 'i' then () else
                    let absoluteIndex = (i * 16) + j
                    let mstart = absoluteIndex - tlen + 1
                    let validStart = alignSpan.Slice(mstart).StartsWith("Twain")
                    if validStart then
                        tc <- tc + 1
        if tc <> 811 then failwith $"invalid count: {tc}"








[<BenchmarkDotNet.Attributes.MemoryDiagnoser>]
[<ShortRunJob>]
type Prefix1() =
    // inherit StringPrefix("Twain")
    inherit StringPrefix("there")


module Sbre.Benchmarks.Optimizations

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Runtime.Intrinsics
open System.Runtime.Intrinsics.X86
open BenchmarkDotNet.Attributes
open Sbre
open Sbre.Benchmarks.Jobs
open Sbre.Optimizations
open System
open Sbre.Pat
open Sbre.Types
open System.Text.Json.Nodes
open System.Buffers
let fullInput = __SOURCE_DIRECTORY__ + "/data/input-text.txt" |> System.IO.File.ReadAllText
// let fullInput = __SOURCE_DIRECTORY__ + "/data/sherlock.txt" |> System.IO.File.ReadAllText
// let fullInput = __SOURCE_DIRECTORY__ + "/data/rust-src-tools-3b0d4813.txt" |> System.IO.File.ReadAllText

let frequenciesJsonText = __SOURCE_DIRECTORY__ + "/data/charFreqWithControl.json"  |> System.IO.File.ReadAllText

let testInput =
                // "Lorem there have ipsum"
                fullInput
                // |> String.replicate 10
                // |> String.replicate 100


module Patterns =

    // rust-src-tools-3b0d4813.txt
    // TODO: bitvector error
    let DATE = System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + "/data/pattern-date.txt" )
    
    // rust-src-tools-3b0d4813.txt
    let URL = System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + "/data/pattern-url.txt" )
    
    [<Literal>] // en-sampled.txt
    let SHERLOCK = @"Sherlock Holmes|John Watson|Irene Adler|Inspector Lestrade|Professor Moriarty"
    [<Literal>] // en-sampled.txt
    let SHERLOCK_CASEIGNORE = @"(?i)Sherlock Holmes|John Watson|Irene Adler|Inspector Lestrade|Professor Moriarty"

    [<Literal>] // twain
    let WORD_END = @"\w+nn\W" // \b\w+nn\b

    [<Literal>] // twain
    let HAVE_THERE = ".*have.*&.*there.*"

    [<Literal>] // twain
    let TWAIN = "Twain"

    [<Literal>] // twain
    let TWAIN_CASEIGNORE = "(?i)Twain"

    [<Literal>] // twain
    let AZ_SHING = "[a-z]shing"

    [<Literal>] // twain
    let HUCK_SAW = @"Huck[a-zA-Z]+|Saw[a-zA-Z]+"

    [<Literal>] // twain
    let AQ_X = "[a-q][^u-z]{13}x"

    [<Literal>] // twain
    let TOM_SAWYER_HUCKLEBERRY_FINN = "Tom|Sawyer|Huckleberry|Finn"

    [<Literal>] // twain
    let TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE = "(?i)Tom|Sawyer|Huckleberry|Finn"

    [<Literal>] // twain
    let D02_TOM_SAWYER_HUCKLEBERRY_FINN = ".{0,2}(Tom|Sawyer|Huckleberry|Finn)"

    [<Literal>] // twain
    let D24_TOM_SAWYER_HUCKLEBERRY_FINN = ".{2,4}(Tom|Sawyer|Huckleberry|Finn)"

    [<Literal>] // twain
    let TOM_RIVER = "Tom.{10,25}river|river.{10,25}Tom"

    [<Literal>] // twain
    let AZ_ING = "[a-zA-Z]+ing"

    [<Literal>] // twain
    let AZ_ING_SPACES = "\s[a-zA-Z]{0,12}ing\s"

    [<Literal>] // twain
    let AZ_AWYER_INN = "([A-Za-z]awyer|[A-Za-z]inn)\s"

    [<Literal>] // twain
    let QUOTES = @"[""'][^""']{0,30}[?!\.][""']"

    [<Literal>] // twain
    let HUCK_AZ = @"Huck[A-Za-z]"



let loadJsonCharFrequencies (jsonText: string) =
    let json = JsonValue.Parse jsonText
    (json.Item "characters").AsArray() |> Seq.map (fun charFreq ->
        ((charFreq.Item "character").GetValue<char>(), (charFreq.Item "frequency").GetValue<float>())
        ) |> dict

let characterFreq = loadJsonCharFrequencies frequenciesJsonText




[<MemoryDiagnoser(true)>]
// [<ShortRunJob>]
type PrefixCharsetSearch () =

    [<Params(
        // Twain regexes
        
        // Patterns.WORD_END,
        // Patterns.HAVE_THERE,
        // Patterns.TWAIN,
        // Patterns.TWAIN_CASEIGNORE,
        // Patterns.AZ_SHING,
        // Patterns.HUCK_SAW,
        // Patterns.AQ_X,
        // Patterns.TOM_SAWYER_HUCKLEBERRY_FINN,
        // Patterns.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE,
        // Patterns.D02_TOM_SAWYER_HUCKLEBERRY_FINN,
        // Patterns.D24_TOM_SAWYER_HUCKLEBERRY_FINN,
        // Patterns.TOM_RIVER,
        // Patterns.AZ_ING,
        Patterns.AZ_ING_SPACES,
        // Patterns.AZ_AWYER_INN,
        Patterns.QUOTES
        
        // Sherlock regexes
        
        // Patterns.SHERLOCK,
        // Patterns.SHERLOCK_CASEIGNORE,
        // Patterns.WORD_END,
        // Patterns.HAVE_THERE,
        // Patterns.AZ_SHING,
        // Patterns.AQ_X,
        // Patterns.AZ_ING,
        // Patterns.AZ_ING_SPACES,
        // Patterns.QUOTES
    )>]
    member val rs: string = Patterns.AZ_SHING with get, set
    // member val rs: string = Patterns.SHERLOCK_CASEIGNORE with get, set
    
    member val regex: Regex = Regex("") with get, set

    [<GlobalSetup(Target = "NoSkip")>]
    member this.NoSkipSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.NoOptimization)

    // [<Benchmark>]
    member this.NoSkip() =
        this.regex.Count(testInput)

    
    [<GlobalSetup(Target = "SetsSuffix")>]
    member this.SetsSuffixSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.ExactSets)

    // [<Benchmark>]
    member this.SetsSuffix() =
        this.regex.Count(testInput)
        
        
    [<GlobalSetup(Target = "WeightedSetsSimple")>]
    member this.WeightedSetsSimpleSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)

    // [<Benchmark>]
    member this.WeightedSetsSimple() =
        this.regex.Count(testInput)
        
        
    [<GlobalSetup(Target = "WeightedSetsFromFile")>]
    member this.WeightedSetsFromFileSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)
        this.regex.TSetMatcher.SetCharacterWeights(characterFreq)

    // [<Benchmark>]
    member this.WeightedSetsFromFile() =
        this.regex.Count(testInput)
        
        
    [<GlobalSetup(Target = "Exact")>]
    member this.ExactSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.ExactSets)
        this.regex.TSetMatcher.SetCharacterWeights(characterFreq)

    [<Benchmark>]
    member this.Exact() =
        this.regex.Count(testInput)
        
        
    [<GlobalSetup(Target = "Approximate")>]
    member this.ApproximateSetup() =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.ApproximateSets)
        this.regex.TSetMatcher.SetCharacterWeights(characterFreq)

    [<Benchmark>]
    member this.Approximate() =
        this.regex.Count(testInput)
        
        
        
        
    member this.testSetup () =
        this.regex <- Regex(this.rs)
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)
        this.regex.TSetMatcher.SetCharacterWeights(characterFreq)
        ()
        
    member this.testRun () =
        let c = this.regex.Count(testInput)
        ()
        
    member this.testWhole () =
        // let regexes = [
        //     Patterns.WORD_END
        //     Patterns.HAVE_THERE
        //     Patterns.TWAIN
        //     Patterns.TWAIN_CASEIGNORE
        //     Patterns.AZ_SHING
        //     Patterns.HUCK_SAW
        //     Patterns.AQ_X
        //     Patterns.TOM_SAWYER_HUCKLEBERRY_FINN
        //     Patterns.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE
        //     Patterns.D02_TOM_SAWYER_HUCKLEBERRY_FINN
        //     Patterns.D24_TOM_SAWYER_HUCKLEBERRY_FINN
        //     Patterns.TOM_RIVER
        //     Patterns.AZ_ING
        //     Patterns.AZ_ING_SPACES
        //     Patterns.AZ_AWYER_INN
        //     Patterns.QUOTES
        // ]
        // for regexStr in regexes do
        //     let regexOld = Regex(regexStr)
        //     regexOld.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.ApproximateSets)
        //     let c1 = regexOld.Count(testInput)
        //     let regexNew = Regex(regexStr)
        //     regexNew.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.WeightedApproximateSets)
        //     let c2 = regexOld.Count(testInput)
        //     assert (c1 = c2)
        
        
        this.regex <- Regex(@"H[A-Za-z]ck[A-Za-z]")
        // this.regex <- Regex(@"(?i)ips")
        // this.regex <- Regex(@"[""'][^""']{0,30}[?!\.][""']")
        this.regex.TSetMatcher.SetStartSearchOptimization(StartSearchOptimization.ApproximateSets)
        this.regex.TSetMatcher.SetCharacterWeights(characterFreq)
        // let c1 = this.regex.Count(testInput) // 97
        let c1 = this.regex.Count("Lorem Huckle dolor")
        
        
        // this.regex.TSetMatcher.StartSearchMode <- StartSearchOptimization.Original
        // let c2 = this.regex.Count(testInput)
        // assert (c1 = c2)
        ()
            
        
        




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
        let span = fullInput.AsSpan()
        let mutable currpos = fullInput.Length
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
        let origspan = fullInput.AsSpan()
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


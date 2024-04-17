module Sbre.Benchmarks.Optimizations

open System.Globalization
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Columns
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Exporters.Csv
open BenchmarkDotNet.Reports
open BenchmarkDotNet.Validators
open Perfolizer.Horology
open Sbre
open Sbre.Benchmarks.Jobs
open Sbre.Optimizations
open System
open Sbre.Pat
open Sbre.Types

let twain = __SOURCE_DIRECTORY__ + "/data/input-text.txt" |> System.IO.File.ReadAllText
// let sherlock = __SOURCE_DIRECTORY__ + "/data/sherlock.txt" |> System.IO.File.ReadAllText
// let rust = __SOURCE_DIRECTORY__ + "/data/rust-src-tools-3b0d4813.txt" |> System.IO.File.ReadAllText

let testInput =
                // rust
                twain
                // sherlock
                // |> String.replicate 10
                // |> String.replicate 100


module Patterns =
    
    // Patterns from Rebar

    // rust-src-tools-3b0d4813.txt
    // TODO: bitvector error
    // let DATE = System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + "/data/pattern-date.txt" )
    
    // rust-src-tools-3b0d4813.txt
    // let URL = System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + "/data/pattern-url.txt" )
    
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
    let WORD_END = @"\b\w+nn\b" // Similar but differentx: \w+nn\W

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
// [<ShortRunJob>]
type MatchStartOptimization () =
    

    [<Params(
        // Twain regexes
        
        Patterns.TWAIN,
        Patterns.TWAIN_CASEIGNORE,
        Patterns.AZ_SHING,
        Patterns.HUCK_SAW,
        // Patterns.WORD_END,
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
    

    [<GlobalSetup(Target = "Original")>]
    member this.OriginalSetup() =
        this.regex <- Regex(this.rs)

    [<Benchmark>]
    member this.Original() =
        this.regex.Count(testInput)
        
    
    member this.ManualTesting() =
        this.MatchCountTesting()
    
    member this.MatchCountTesting() =
        let pats = [|
            Patterns.TWAIN // 811
            Patterns.TWAIN_CASEIGNORE // 965
            Patterns.AZ_SHING // 1540
            Patterns.HUCK_SAW // 262
            // Patterns.WORD_END // Broken in this version
            Patterns.AQ_X // 4094, Rider finds 4081
            Patterns.TOM_SAWYER_HUCKLEBERRY_FINN // 2598
            Patterns.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE // 4152
            Patterns.D02_TOM_SAWYER_HUCKLEBERRY_FINN // 2598
            Patterns.D24_TOM_SAWYER_HUCKLEBERRY_FINN // 1976
            Patterns.TOM_RIVER // 2
            Patterns.AZ_ING // 78 423 TODO
            Patterns.AZ_ING_SPACES // 55 248 TODO
            Patterns.AZ_AWYER_INN // 209
            Patterns.QUOTES // 8885, Rider finds 8927
           
            Patterns.HAVE_THERE // 426
            Patterns.HUCK_AZ // 56
            Patterns.AZ_UCK_AZ // 706
            Patterns.H_AZ_CK_AZ // 97
        |]
        for pat in pats do
            this.regex <- Regex(pat)
            let count = this.regex.Count(testInput)
            ()
        


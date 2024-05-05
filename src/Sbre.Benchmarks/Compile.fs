module Sbre.Benchmarks.Compile


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
open Sbre.Optimizations
open System
open Sbre.Pat
open Sbre.Types

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


module PatternsTwain =
    
    [<Literal>]
    let TWAIN = @"Twain"

    [<Literal>]
    let TWAIN_CASEIGNORE = @"(?i)Twain"

    [<Literal>]
    let AZ_SHING = @"[a-z]shing"

    [<Literal>]
    let HUCK_SAW = @"Huck[a-zA-Z]+|Saw[a-zA-Z]+"
    
    [<Literal>]
    let WORD_END = @"\w+nn\W" // Alternative version of \b\w+nn\b

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
    
    // Extra patterns, some may be left out of the final results

    [<Literal>]
    let HUCK_AZ = @"Huck[A-Za-z]+"

    [<Literal>]
    let AZ_UCK_AZ = @"[A-Za-z]uck[A-Za-z]+"

    [<Literal>]
    let H_AZ_CK_AZ = @"H[A-Za-z]ck[A-Za-z]+"


[<Config(typedefof<BenchmarkConfig>)>]
[<MemoryDiagnoser(true)>]
// [<ShortRunJob>]
type CompileTimeTwain () =
    

    [<Params(
        PatternsTwain.TWAIN,
        PatternsTwain.TWAIN_CASEIGNORE,
        PatternsTwain.AZ_SHING,
        PatternsTwain.HUCK_SAW,
        PatternsTwain.WORD_END,
        PatternsTwain.AQ_X,
        PatternsTwain.TOM_SAWYER_HUCKLEBERRY_FINN,
        PatternsTwain.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE,
        PatternsTwain.D02_TOM_SAWYER_HUCKLEBERRY_FINN,
        PatternsTwain.D24_TOM_SAWYER_HUCKLEBERRY_FINN,
        PatternsTwain.TOM_RIVER,
        PatternsTwain.AZ_ING,
        PatternsTwain.AZ_ING_SPACES,
        PatternsTwain.AZ_AWYER_INN,
        PatternsTwain.QUOTES
    )>]
    member val rs: string = "" with get, set
    
    member val matchingTextMap = Map [
        PatternsTwain.TWAIN, "Twain"
        PatternsTwain.TWAIN_CASEIGNORE, "Twain"
        PatternsTwain.AZ_SHING, "ashing"
        PatternsTwain.HUCK_SAW, "Huckleberry"
        PatternsTwain.WORD_END, "ann "
        PatternsTwain.AQ_X, "abbbbbbbbbbbbbx"
        PatternsTwain.TOM_SAWYER_HUCKLEBERRY_FINN, "Huckleberry"
        PatternsTwain.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE, "Huckleberry"
        PatternsTwain.D02_TOM_SAWYER_HUCKLEBERRY_FINN, "aaHuckleberry"
        PatternsTwain.D24_TOM_SAWYER_HUCKLEBERRY_FINN, "aaaaHuckleberry"
        PatternsTwain.TOM_RIVER, "Tomaaaaaaaaaaaaaaaaaaaaaaaaariver"
        PatternsTwain.AZ_ING, "aing"
        PatternsTwain.AZ_ING_SPACES, " aaaaaaaaaaaaing "
        PatternsTwain.AZ_AWYER_INN, "Sawyer "
        PatternsTwain.QUOTES, "'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.'"
    ]
    
    member val matchingText: string = "" with get, set
    
    [<GlobalSetup(Target = "RESharp")>]
    member this.RESharpSetup() =
        this.matchingText <- this.matchingTextMap[this.rs]

    [<Benchmark>]
    member this.RESharp() =
        let regex = Regex(this.rs)
        regex.IsMatch(this.matchingText) |> ignore

    [<Benchmark>]
    member this.DotNetCompiled() =
        let regex = System.Text.RegularExpressions.Regex(
            this.rs,
            options = System.Text.RegularExpressions.RegexOptions.Compiled
        )
        ()

    [<Benchmark>]
    member this.DotNetNonBacktracking() =
        let regex = System.Text.RegularExpressions.Regex(
            this.rs,
            options = System.Text.RegularExpressions.RegexOptions.NonBacktracking
        )
        ()
        

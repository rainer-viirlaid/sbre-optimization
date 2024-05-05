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
let tammsaare = __SOURCE_DIRECTORY__ + "/data/Tammsaare Kollektsioon.txt" |> System.IO.File.ReadAllText

let estWikiLocation = __SOURCE_DIRECTORY__ + "/data/estWikiLoc.txt" |> System.IO.File.ReadAllText |> _.Trim()
let estWiki = if estWikiLocation <> "" then estWikiLocation
                                            |> System.IO.File.ReadAllBytes
                                            |> System.Text.Encoding.UTF8.GetChars
                else [||]
let engWikiLocation = __SOURCE_DIRECTORY__ + "/data/engWikiLoc.txt" |> System.IO.File.ReadAllText |> _.Trim()
let engWiki = if engWikiLocation <> "" then engWikiLocation
                                            |> System.IO.File.ReadAllBytes
                                            |> System.Text.Encoding.UTF8.GetChars
                else [||]


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


module PatternsTammsaare =
    
    [<Literal>]
    let TAMMSAARE = @"Tammsaare"
    
    [<Literal>]
    let TAMMSAARE_CASEIGNORE = @"(?i)Tammsaare"
    
    [<Literal>]
    let AZ_MINE = @"[a-zšžõäöü]mine"
    
    [<Literal>]
    let ANDR_PEAR_AZ = @"Andr[A-ZŠŽÕÄÖÜa-zšžõäöü]+|Pear[A-ZŠŽÕÄÖÜa-zšžõäöü]+"
    
    [<Literal>]
    let WORD_END = @"\w+nn\W"
    
    [<Literal>]
    let AQ_W = @"[a-q][^u-z]{13}w"
    
    [<Literal>]
    let KARL_ANDRES_PEARU_VANAPAGAN = @"Karl|Andres|Pearu|Vanapagan"
    
    [<Literal>]
    let KARL_ANDRES_PEARU_VANAPAGAN_CASEIGNORE = @"(?i)Karl|Andres|Pearu|Vanapagan"
    
    [<Literal>]
    let D02_KARL_ANDRES_PEARU_VANAPAGAN = @".{0,2}(Karl|Andres|Pearu|Vanapagan)"
    
    [<Literal>]
    let D24_KARL_ANDRES_PEARU_VANAPAGAN = @".{2,4}(Karl|Andres|Pearu|Vanapagan)"
    
    [<Literal>]
    let MADIS_KRAAV = @"Madis.{10,25}kraav|kraav.{10,25}Madis"
    
    [<Literal>]
    let AZ_NE = @"[A-ZŠŽÕÄÖÜa-zšžõäöü]+ne"
    
    [<Literal>]
    let AZ_NE_SPACES = @"\s[A-ZŠŽÕÄÖÜa-zšžõäöü]{0,12}ne\s"
    
    [<Literal>]
    let AZ_EARU_NDRES = @"([A-ZŠŽÕÄÖÜa-zšžõäöü]earu|[A-ZŠŽÕÄÖÜa-zšžõäöü]ndres)\s"
    
    [<Literal>]
    let QUOTES = @"[„""'][^“""']{0,30}[?!\.][“""']"
    
    
module PatternsEstWiki =
    
    [<Literal>]
    let EESTI = @"Eesti"
    
    [<Literal>]
    let ROOTSI = @"Rootsi"
    
    [<Literal>]
    let EESTI_CASEIGNORE = @"(?i)Eesti"
    
    [<Literal>]
    let AZ_EE = @"[a-zšžüõöä]ee"
    
    [<Literal>]
    let HELI_AJA_AZ = @"Heli[a-zA-ZšžüõöäŠŽÜÕÖÄ]+|Aja[a-zA-ZšžüõöäŠŽÜÕÖÄ]+"
    
    [<Literal>]
    let AQ_X = @"[a-q][^u-z]{12}x"
    
    [<Literal>]
    let TOOMAS_MARGUS_REIN_JAAN = @"Toomas|Margus|Rein|Jaan"
    
    [<Literal>]
    let TOOMAS_MARGUS_REIN_JAAN_CASEIGNORE = @"(?i)Toomas|Margus|Rein|Jaan"
    
    [<Literal>]
    let D02_TOOMAS_MARGUS_REIN_JAAN = @".{0,2}Toomas|Margus|Rein|Jaan"
    
    [<Literal>]
    let D24_TOOMAS_MARGUS_REIN_JAAN = @".{2,4}Toomas|Margus|Rein|Jaan"
    
    [<Literal>]
    let EESTI_JOGI = @"Eesti.{10,25}jõgi|jõgi.{10,25}Eesti"
    
    [<Literal>]
    let AZ_TUD = @"[a-zA-ZšžüõöäŠŽÜÕÖÄ]+tud"
    
    [<Literal>]
    let AZ_TUD_SPACES = @"\s[a-zA-ZšžüõöäŠŽÜÕÖÄ]{0,12}tud\s"
    
    [<Literal>]
    let AZ_INA_EIN = @"([A-Za-zšžüõöäŠŽÜÕÖÄ]ina|[A-Za-zšžüõöäŠŽÜÕÖÄ]ein)\s"
    
    [<Literal>]
    let QUOTES = @"[""'][^""']{0,31}[?!\.][""']"
    
    [<Literal>]
    let CURRENCY = @"\p{Sc}"
    
    
module PatternsEngWiki =
    
    [<Literal>]
    let LINCOLN = @"Lincoln"
    
    [<Literal>]
    let LINCOLN_CASEIGNORE = @"(?i)Lincoln"
    
    [<Literal>]
    let AZ_SHING = @"[a-z]shing"
    
    [<Literal>]
    let LINC_ROO = @"Linc[a-zA-Z]+|Roo[a-zA-Z]+"
    
    [<Literal>]
    let WORD_END = @"\w+nn\W" // Alternative version of \b\w+nn\b
    
    [<Literal>]
    let AQ_X = @"[a-q][^u-z]{13}x"
    
    [<Literal>]
    let PRESIDENTS_ALTERNATION = @"Lincoln|Washington|Roosevelt|Jefferson"
    
    [<Literal>]
    let PRESIDENTS_ALTERNATION_CASEIGNORE = @"(?i)Lincoln|Washington|Roosevelt|Jefferson"
    
    [<Literal>]
    let D02_PRESIDENTS_ALTERNATION = @".{0,2}(Lincoln|Washington|Roosevelt|Jefferson)"
    
    [<Literal>]
    let D24_PRESIDENTS_ALTERNATION = @".{2,4}(Lincoln|Washington|Roosevelt|Jefferson)"
    
    [<Literal>]
    let ROOSEVELT_RIVER = @"Roosevelt.{10,25}river|river.{10,25}Roosevelt"
    
    [<Literal>]
    let AZ_ING = @"[a-zA-Z]+ing"
    
    [<Literal>]
    let AZ_ING_SPACES = @"\s[a-zA-Z]{0,12}ing\s"
    
    [<Literal>]
    let AZ_INCOLN_OOSEVELT = @"([A-Za-z]incoln|[A-Za-z]oosevelt)\s"
    
    [<Literal>]
    let QUOTES = @"[""'][^""']{0,30}[?!\.][""']"


type BenchmarkConfig() as self =
    inherit ManualConfig() 
    do
        let summaryStyle = SummaryStyle(CultureInfo.InvariantCulture, true, SizeUnit.B, TimeUnit.Millisecond, false)
                            .WithMaxParameterColumnWidth(60)
        self.SummaryStyle <- summaryStyle
        self
            .AddColumn([| StatisticColumn.Error; StatisticColumn.Median |])
            .AddExporter(CsvExporter(CsvSeparator.Comma, summaryStyle))
            // .With(CsvExporter(CsvSeparator.Comma, summaryStyle))
            |> ignore



[<Config(typedefof<BenchmarkConfig>)>]
[<MemoryDiagnoser(true)>]
// [<ShortRunJob>]
type MatchStartOptimizationTwain () =
    

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
        PatternsTwain.QUOTES,

        PatternsTwain.HUCK_AZ,
        PatternsTwain.AZ_UCK_AZ,
        PatternsTwain.H_AZ_CK_AZ
    )>]
    member val rs: string = "" with get, set
    
    member val regex: Regex = Regex("") with get, set
    
    [<GlobalSetup(Target = "Original")>]
    member this.OriginalSetup() =
        this.regex <- Regex(this.rs)

    [<Benchmark>]
    member this.Original() =
        this.regex.Count(twain)
        


[<Config(typedefof<BenchmarkConfig>)>]
[<MemoryDiagnoser(true)>]
// [<ShortRunJob>]
type MatchStartOptimizationTammsaare () =
    

    [<Params(
        PatternsTammsaare.TAMMSAARE,
        PatternsTammsaare.TAMMSAARE_CASEIGNORE,
        PatternsTammsaare.AZ_MINE,
        PatternsTammsaare.ANDR_PEAR_AZ,
        PatternsTammsaare.WORD_END,
        PatternsTammsaare.AQ_W,
        PatternsTammsaare.KARL_ANDRES_PEARU_VANAPAGAN,
        PatternsTammsaare.KARL_ANDRES_PEARU_VANAPAGAN_CASEIGNORE,
        PatternsTammsaare.D02_KARL_ANDRES_PEARU_VANAPAGAN,
        PatternsTammsaare.D24_KARL_ANDRES_PEARU_VANAPAGAN,
        PatternsTammsaare.MADIS_KRAAV,
        PatternsTammsaare.AZ_NE,
        PatternsTammsaare.AZ_NE_SPACES,
        PatternsTammsaare.AZ_EARU_NDRES,
        PatternsTammsaare.QUOTES
    )>]
    member val rs: string = "" with get, set
    
    member val regex: Regex = Regex("") with get, set
    
    [<GlobalSetup(Target = "Original")>]
    member this.OriginalSetup() =
        this.regex <- Regex(this.rs)

    [<Benchmark>]
    member this.Original() =
        this.regex.Count(tammsaare)
        


[<Config(typedefof<BenchmarkConfig>)>]
[<MemoryDiagnoser(true)>]
// [<ShortRunJob>]
type MatchStartOptimizationEstWiki () =
    

    [<Params(
        PatternsEstWiki.EESTI,
        PatternsEstWiki.ROOTSI,
        PatternsEstWiki.EESTI_CASEIGNORE,
        PatternsEstWiki.AZ_EE,
        PatternsEstWiki.HELI_AJA_AZ,
        PatternsEstWiki.AQ_X,
        PatternsEstWiki.TOOMAS_MARGUS_REIN_JAAN,
        PatternsEstWiki.TOOMAS_MARGUS_REIN_JAAN_CASEIGNORE,
        PatternsEstWiki.D02_TOOMAS_MARGUS_REIN_JAAN,
        PatternsEstWiki.D24_TOOMAS_MARGUS_REIN_JAAN,
        PatternsEstWiki.EESTI_JOGI,
        PatternsEstWiki.AZ_TUD,
        PatternsEstWiki.AZ_TUD_SPACES,
        PatternsEstWiki.AZ_INA_EIN,
        PatternsEstWiki.QUOTES,
        PatternsEstWiki.CURRENCY
    )>]
    member val rs: string = "" with get, set
    
    member val regex: Regex = Regex("") with get, set
    
    [<GlobalSetup(Target = "Original")>]
    member this.OriginalSetup() =
        this.regex <- Regex(this.rs)

    [<Benchmark>]
    member this.Original() =
        this.regex.Count(estWiki)
        


[<Config(typedefof<BenchmarkConfig>)>]
[<MemoryDiagnoser(true)>]
// [<ShortRunJob>]
type MatchStartOptimizationEngWiki () =
    

    [<Params(
        PatternsEngWiki.LINCOLN,
        PatternsEngWiki.LINCOLN_CASEIGNORE,
        PatternsEngWiki.AZ_SHING,
        PatternsEngWiki.LINC_ROO,
        PatternsEngWiki.WORD_END,
        PatternsEngWiki.AQ_X,
        PatternsEngWiki.PRESIDENTS_ALTERNATION,
        PatternsEngWiki.PRESIDENTS_ALTERNATION_CASEIGNORE,
        PatternsEngWiki.D02_PRESIDENTS_ALTERNATION,
        PatternsEngWiki.D24_PRESIDENTS_ALTERNATION,
        PatternsEngWiki.ROOSEVELT_RIVER,
        PatternsEngWiki.AZ_ING,
        PatternsEngWiki.AZ_ING_SPACES,
        PatternsEngWiki.AZ_INCOLN_OOSEVELT,
        PatternsEngWiki.QUOTES
    )>]
    member val rs: string = "" with get, set
    
    member val regex: Regex = Regex("") with get, set
    
    [<GlobalSetup(Target = "Original")>]
    member this.OriginalSetup() =
        this.regex <- Regex(this.rs)

    [<Benchmark>]
    member this.Original() =
        this.regex.Count(engWiki)
        
        
        

type MatchCountingCorrectness () =
    
    member this.ManualTesting() =
        // this.MatchCountTestingEstWiki()
        this.MatchCountTestingEngWiki()
    
    member this.MatchCountTestingTwain() =
        let pats = [|
            PatternsTwain.TWAIN             // 811
            PatternsTwain.TWAIN_CASEIGNORE  // 965
            PatternsTwain.AZ_SHING          // 1540
            PatternsTwain.HUCK_SAW          // 262
            PatternsTwain.WORD_END          // 262
            PatternsTwain.AQ_X              // 4094, Rider finds 4081
            PatternsTwain.TOM_SAWYER_HUCKLEBERRY_FINN            // 2598
            PatternsTwain.TOM_SAWYER_HUCKLEBERRY_FINN_CASEIGNORE // 4152
            PatternsTwain.D02_TOM_SAWYER_HUCKLEBERRY_FINN        // 2598
            PatternsTwain.D24_TOM_SAWYER_HUCKLEBERRY_FINN        // 1976
            PatternsTwain.TOM_RIVER         // 2
            PatternsTwain.AZ_ING            // 78 423, too many for Rider
            PatternsTwain.AZ_ING_SPACES     // 55 248, too many for Rider
            PatternsTwain.AZ_AWYER_INN      // 209
            PatternsTwain.QUOTES            // 8885, Rider finds 8927
           
            PatternsTwain.HUCK_AZ           // 56
            PatternsTwain.AZ_UCK_AZ         // 706
            PatternsTwain.H_AZ_CK_AZ        // 97
        |]
        for pat in pats do
            let regex = Regex(pat)
            let count = regex.Count(twain)
            ()
        for pat in pats do
            let compiled = System.Text.RegularExpressions.Regex(
                pat,
                options = System.Text.RegularExpressions.RegexOptions.Compiled,
                matchTimeout = TimeSpan.FromMilliseconds(10_000.)
            )
            let count = compiled.Count(twain)
            ()
    
    member this.MatchCountTestingTammsaare() =
        let pats = [|
            PatternsTammsaare.TAMMSAARE             // 15
            PatternsTammsaare.TAMMSAARE_CASEIGNORE  // 22
            PatternsTammsaare.AZ_MINE               // 1603
            PatternsTammsaare.ANDR_PEAR_AZ          // 3420
            PatternsTammsaare.WORD_END              // 260, Rider finds 129
            PatternsTammsaare.AQ_W                  // 2133, Rider finds 2172
            PatternsTammsaare.KARL_ANDRES_PEARU_VANAPAGAN            // 4049
            PatternsTammsaare.KARL_ANDRES_PEARU_VANAPAGAN_CASEIGNORE // 4051
            PatternsTammsaare.D02_KARL_ANDRES_PEARU_VANAPAGAN        // 4049
            PatternsTammsaare.D24_KARL_ANDRES_PEARU_VANAPAGAN        // 3800
            PatternsTammsaare.MADIS_KRAAV           // 9
            PatternsTammsaare.AZ_NE                 // 20994, Too many for Rider
            PatternsTammsaare.AZ_NE_SPACES          // 8782
            PatternsTammsaare.AZ_EARU_NDRES         // 1845
            PatternsTammsaare.QUOTES                // 4923
        |]
        for pat in pats do
            let regex = Regex(pat)
            let count = regex.Count(tammsaare)
            ()
        for pat in pats do
            let compiled = System.Text.RegularExpressions.Regex(
                pat,
                options = System.Text.RegularExpressions.RegexOptions.Compiled,
                matchTimeout = TimeSpan.FromMilliseconds(10_000.)
            )
            let count = compiled.Count(tammsaare)
            ()
    
    member this.MatchCountTestingEstWiki() =
        let pats = [|
            PatternsEstWiki.EESTI            // 589249
            PatternsEstWiki.ROOTSI           // 56211
            PatternsEstWiki.EESTI_CASEIGNORE // 733450
            PatternsEstWiki.AZ_EE            // 2545307
            PatternsEstWiki.HELI_AJA_AZ      // 54998, sbre timeout
            PatternsEstWiki.AQ_X             // 777653
            PatternsEstWiki.TOOMAS_MARGUS_REIN_JAAN             // 83711
            PatternsEstWiki.TOOMAS_MARGUS_REIN_JAAN_CASEIGNORE  // 246103
            PatternsEstWiki.D02_TOOMAS_MARGUS_REIN_JAAN         // 83711
            PatternsEstWiki.D24_TOOMAS_MARGUS_REIN_JAAN         // 83408
            PatternsEstWiki.EESTI_JOGI       // 206
            PatternsEstWiki.AZ_TUD           // 842963
            PatternsEstWiki.AZ_TUD_SPACES    // 540338, sbre slow
            PatternsEstWiki.AZ_INA_EIN       // 175971
            PatternsEstWiki.QUOTES           // 30371
            PatternsEstWiki.CURRENCY         // 8674
        |]
        for pat in pats do
            let regex = Regex(pat)
            let count = regex.Count(estWiki)
            ()
        for pat in pats do
            let compiled = System.Text.RegularExpressions.Regex(
                pat,
                options = System.Text.RegularExpressions.RegexOptions.Compiled
                // , matchTimeout = TimeSpan.FromMilliseconds(20_000.)
            )
            let count = compiled.Count(estWiki)
            ()
    
    member this.MatchCountTestingEngWiki() =
        let pats = [|
            PatternsEngWiki.LINCOLN             // 7400
            PatternsEngWiki.LINCOLN_CASEIGNORE  // 8125
            PatternsEngWiki.AZ_SHING            // 75741
            PatternsEngWiki.LINC_ROO            // 19409
            PatternsEngWiki.WORD_END            // 73464
            PatternsEngWiki.AQ_X                // 463385
            PatternsEngWiki.PRESIDENTS_ALTERNATION              // 42464
            PatternsEngWiki.PRESIDENTS_ALTERNATION_CASEIGNORE   // 51644
            PatternsEngWiki.D02_PRESIDENTS_ALTERNATION          // 42464
            PatternsEngWiki.D24_PRESIDENTS_ALTERNATION          // 41855
            PatternsEngWiki.ROOSEVELT_RIVER     // 3
            PatternsEngWiki.AZ_ING              // 3095748
            PatternsEngWiki.AZ_ING_SPACES       // 1935049
            PatternsEngWiki.AZ_INCOLN_OOSEVELT  // 6234
            PatternsEngWiki.QUOTES              // 15686
        |]
        for pat in pats do
            let regex = Regex(pat)
            let count = regex.Count(engWiki)
            ()
        for pat in pats do
            let compiled = System.Text.RegularExpressions.Regex(
                pat,
                options = System.Text.RegularExpressions.RegexOptions.Compiled
                // , matchTimeout = TimeSpan.FromMilliseconds(20_000.)
            )
            let count = compiled.Count(engWiki)
            ()
        
        


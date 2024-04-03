#I "../src/Sbre.Test/bin/Debug/net8.0"
#r "RuntimeRegexCopy.dll"
#I "../src/Sbre.Test/bin/Debug/net8.0"
#r "RuntimeRegexCopy.dll"
#r "Sbre.dll"

// open System.Collections.Generic
open System
open System.Buffers
open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.Json.Serialization
open System.Threading
open Microsoft.FSharp.Core
open Microsoft.VisualBasic.CompilerServices
open Sbre
open FSharp.Data
open System.Globalization
open Sbre.Types
open Sbre.Pat
open Sbre.Optimizations


let findLetterFrequency (text: IEnumerable<char>) =
    let counts = new Dictionary<char, int>()
    text |> Seq.iter (fun (c: char) ->
        if not (counts.ContainsKey(c)) then counts.Add(c, 1)
        else counts.Item(c) <- (counts.Item(c) + 1)
        )
    counts
    
let encodeChar (character: char) onlyBMP =
    let code = (int) character
    if 55296 <= code && code <= 57343 then
        if not onlyBMP then
            $"\\u{code:x4}"
        else
            ""
    else
        JsonEncodedText.Encode(Char.ToString(character)).Value
        
let writeCharFrequenciesToJsonFile (charCounts: Dictionary<char,int>) filename fullTextLength onlyBMP =
    let charFreqJsonArray =
        charCounts.Keys
        |> Seq.sortByDescending (fun (c: char) -> charCounts.Item(c))
        |> Seq.map (fun (character) ->
            let count = charCounts.Item(character)
            let charString = encodeChar character onlyBMP
            if charString <> "" then
                $"{{\"character\": \"{charString}\", \"frequency\": %.6f{float 100 * (float count) / (float fullTextLength)}}}"
            else
                ""
        )
        |> Seq.filter (fun str -> str <> "")
        |> String.concat ","
    let fullJson = "{\"characters\": [" + charFreqJsonArray + "]}"
    if onlyBMP then
        let res = (JsonValue.Parse fullJson).ToJsonString(JsonSerializerOptions(WriteIndented = true))
        System.IO.File.WriteAllText(filename, res)
    else
        System.IO.File.WriteAllText(filename, fullJson)
    ()
    

let tammsaare = __SOURCE_DIRECTORY__ + "/samples/Tammsaare Kollektsioon.txt" |> System.IO.File.ReadAllText
let freqs = findLetterFrequency (tammsaare)
printfn "%A" freqs
writeCharFrequenciesToJsonFile freqs (__SOURCE_DIRECTORY__ + "/charFreqTammsaare.json") tammsaare.Length true


let vikipeedia = "" |> System.IO.File.ReadAllText
let freqsV = findLetterFrequency (vikipeedia)
writeCharFrequenciesToJsonFile freqsV (__SOURCE_DIRECTORY__ + "/charFreqVikipeedia.json") vikipeedia.Length true


let sherlock = __SOURCE_DIRECTORY__ + "/sherlock.txt" |> System.IO.File.ReadAllText
let freqsS = findLetterFrequency (sherlock)
writeCharFrequenciesToJsonFile freqsS (__SOURCE_DIRECTORY__ + "/charFreqSherlock.json") sherlock.Length true


let wikipedia1 = "" |> System.IO.File.ReadAllText
let freqsW1 = findLetterFrequency (wikipedia1)
writeCharFrequenciesToJsonFile freqsW1 (__SOURCE_DIRECTORY__ + "/charFreqWikipedia1.json") wikipedia1.Length true


let wikipedia9 = "" |> System.IO.File.ReadAllText
let freqsW9 = findLetterFrequency (wikipedia9)
writeCharFrequenciesToJsonFile freqsW9 (__SOURCE_DIRECTORY__ + "/charFreqWikipedia9.json") wikipedia9.Length true



// Converting to CSV for analysis

let loadJsonCharFrequencies (jsonText: string) =
    let json = JsonValue.Parse jsonText
    (json.Item "characters").AsArray() |> Seq.map (fun charFreq ->
        ((charFreq.Item "character").GetValue<char>(), (charFreq.Item "frequency").GetValue<float>())
        ) |> dict

let charToCsvString character =
    if character = '"' then
        "\"\"\"\""
    else if character = ';' then
        "\";\""
    else if character = '\n' then
        "\\n"
    else if character = '\r' then
        "\\r"
    else if character = ' ' then
        "\" \""
    else
        character.ToString()
    
let convertJsonToCsv inputFile outputFile =
    let freqJson = inputFile |> System.IO.File.ReadAllText
    let freqDict = loadJsonCharFrequencies freqJson
    let csvStr = "Character;Frequency\n" +
                 String.Join("\n", freqDict |> Seq.map (fun pair -> charToCsvString pair.Key + ";" + pair.Value.ToString()))
    System.IO.File.WriteAllText(outputFile, csvStr)


convertJsonToCsv (__SOURCE_DIRECTORY__ + "/charFreqWikipedia9.json") (__SOURCE_DIRECTORY__ + "/charFreqWikipedia9.csv")


// Get all symbols

let findAllChars () =
    let weightFiles = [|
        __SOURCE_DIRECTORY__ + "/charFreqSherlock.json"
        __SOURCE_DIRECTORY__ + "/charFreqTammsaare.json"
        __SOURCE_DIRECTORY__ + "/charFreqTwain.json"
        __SOURCE_DIRECTORY__ + "/charFreqVikipeedia.json"
        __SOURCE_DIRECTORY__ + "/charFreqWikipedia1.json"
        __SOURCE_DIRECTORY__ + "/charFreqWikipedia9.json"
    |]
    let charsSet = new HashSet<char>()
    for inputFile in weightFiles do
        let freqJson = inputFile |> System.IO.File.ReadAllText
        let freqDict = loadJsonCharFrequencies freqJson
        let charSeq = freqDict |> Seq.map (fun pair -> pair.Key)
        for character in charSeq do
            charsSet.Add(character) |> ignore
    let csvStr = "Character\n" + String.Join("\n", charsSet |> Seq.map (fun character -> charToCsvString character))
    let outputFile = __SOURCE_DIRECTORY__ + "/allChars.csv"
    System.IO.File.WriteAllText(outputFile, csvStr)
    
findAllChars()

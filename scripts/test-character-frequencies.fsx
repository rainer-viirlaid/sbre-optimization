#I "../src/Sbre.Test/bin/Debug/net8.0"
#r "RuntimeRegexCopy.dll"
#I "../src/Sbre.Test/bin/Debug/net8.0"
#r "RuntimeRegexCopy.dll"
#r "Sbre.dll"

// open System.Collections.Generic
open System
open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Nodes
open Microsoft.FSharp.Core


let findLetterCounts (text: IEnumerable<char>, textSize: int, symbolCount: int) =
    let counts = Dictionary<char, int>()
    let step = (textSize - 1) / symbolCount
    if step = 0 then
        text |> Seq.iter (fun (c: char) ->
            if not (counts.ContainsKey(c)) then counts.Add(c, 1)
            else counts.Item(c) <- (counts.Item(c) + 1)
            )
    else
        text |> Seq.iteri (fun i c ->
            if i % step = 0 then
                if not (counts.ContainsKey(c)) then counts.Add(c, 1)
                else counts[c] <- (counts.Item(c) + 1)
            )
    counts
    
let encodeChar (character: char) onlyBMP =
    let code = int character
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
        |> Seq.map (fun character ->
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
    
    
// Calculating weights

let twain = (__SOURCE_DIRECTORY__ + "/input-text.txt" |> System.IO.File.ReadAllText)
let symbolCount = 100000
let freqsT = findLetterCounts (twain, twain.Length, symbolCount)
// writeCharFrequenciesToJsonFile freqsT (__SOURCE_DIRECTORY__ + "/charFreqTwain.json") symbolCount true
writeCharFrequenciesToJsonFile freqsT (__SOURCE_DIRECTORY__ + "/charFreqTwain-" + symbolCount.ToString() + ".json") symbolCount true


let tammsaare = (__SOURCE_DIRECTORY__ + "/samples/Tammsaare Kollektsioon.txt" |> System.IO.File.ReadAllText)
let freqs = findLetterCounts (tammsaare, tammsaare.Length, symbolCount)
writeCharFrequenciesToJsonFile freqs (__SOURCE_DIRECTORY__ + "/charFreqTammsaare-" + symbolCount.ToString() + ".json") symbolCount true


let vikipeedia = "" |> System.IO.File.ReadAllText
let freqsV = findLetterCounts (vikipeedia, 0, 0)
writeCharFrequenciesToJsonFile freqsV (__SOURCE_DIRECTORY__ + "/charFreqVikipeedia.json") vikipeedia.Length true


let sherlock = __SOURCE_DIRECTORY__ + "/sherlock.txt" |> System.IO.File.ReadAllText
let freqsS = findLetterCounts (sherlock, 0, 0)
writeCharFrequenciesToJsonFile freqsS (__SOURCE_DIRECTORY__ + "/charFreqSherlock.json") sherlock.Length true


let wikipedia1 = "" |> System.IO.File.ReadAllText
let freqsW1 = findLetterCounts (wikipedia1, 0, 0)
writeCharFrequenciesToJsonFile freqsW1 (__SOURCE_DIRECTORY__ + "/charFreqWikipedia1.json") wikipedia1.Length true


let wikipedia9 = "C:\\Users\\Name\\Documents\\TalTech\\Loputoo\\Wikipedia\\Wikipedia EN 9.txt" |> System.IO.File.ReadAllText
let freqsW9 = findLetterCounts (wikipedia9, wikipedia9.Length, symbolCount)
writeCharFrequenciesToJsonFile freqsW9 (__SOURCE_DIRECTORY__ + "/charFreqWikipedia9-" + symbolCount.ToString() + ".json") symbolCount true



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


convertJsonToCsv (__SOURCE_DIRECTORY__ + "/charFreqTammsaare-100000.json") (__SOURCE_DIRECTORY__ + "/charFreqTammsaare-100000.csv")


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
    let charsSet = HashSet<char>()
    for inputFile in weightFiles do
        let freqJson = inputFile |> System.IO.File.ReadAllText
        let freqDict = loadJsonCharFrequencies freqJson
        let charSeq = freqDict |> Seq.map (_.Key)
        for character in charSeq do
            charsSet.Add(character) |> ignore
    let csvStr = "Character\n" + String.Join("\n", charsSet |> Seq.map charToCsvString)
    let outputFile = __SOURCE_DIRECTORY__ + "/allChars.csv"
    System.IO.File.WriteAllText(outputFile, csvStr)
    
findAllChars()



// Combine multiple weight files into a CSV

let combineIntoCsv (files: string array) (columnNames: string array) (outputFileName: string) =
    let charsSet = HashSet<char>()
    let columnSymbolWeights = Dictionary()
    for i in 0..files.Length - 1 do
        let freqJson = files[i] |> System.IO.File.ReadAllText
        let freqDict = loadJsonCharFrequencies freqJson
        columnSymbolWeights.Add(columnNames[i], freqDict)
        let charSeq = freqDict |> Seq.map (_.Key)
        for character in charSeq do
            charsSet.Add(character) |> ignore
    let mutable csvStr = "Character;" + String.Join(";", columnNames)
    for character in charsSet do
        csvStr <- csvStr + "\n" + charToCsvString character + ";" + String.Join(";", columnNames |> Seq.map (fun col ->
            let weights = columnSymbolWeights[col]
            if weights.ContainsKey(character) then
                weights[character].ToString()
            else
                "0"
            ))
    let outputFile = __SOURCE_DIRECTORY__ + "/" + outputFileName
    System.IO.File.WriteAllText(outputFile, csvStr)
    
    

let columns = [|
    "100 symbols"
    "1000 symbols"
    "10000 symbols"
    "100000 symbols"
    "All symbols"
|]

let tammsaareWeights = [|
    __SOURCE_DIRECTORY__ + "/charFreqTammsaare-100.json"
    __SOURCE_DIRECTORY__ + "/charFreqTammsaare-1000.json"
    __SOURCE_DIRECTORY__ + "/charFreqTammsaare-10000.json"
    __SOURCE_DIRECTORY__ + "/charFreqTammsaare-100000.json"
    __SOURCE_DIRECTORY__ + "/charFreqTammsaare.json"
|]

let wiki9Weights = [|
    __SOURCE_DIRECTORY__ + "/charFreqWikipedia9-100.json"
    __SOURCE_DIRECTORY__ + "/charFreqWikipedia9-1000.json"
    __SOURCE_DIRECTORY__ + "/charFreqWikipedia9-10000.json"
    __SOURCE_DIRECTORY__ + "/charFreqWikipedia9-100000.json"
    __SOURCE_DIRECTORY__ + "/charFreqWikipedia9.json"
|]

let twainWeights = [|
    __SOURCE_DIRECTORY__ + "/charFreqTwain-100.json"
    __SOURCE_DIRECTORY__ + "/charFreqTwain-1000.json"
    __SOURCE_DIRECTORY__ + "/charFreqTwain-10000.json"
    __SOURCE_DIRECTORY__ + "/charFreqTwain-100000.json"
    __SOURCE_DIRECTORY__ + "/charFreqTwain.json"
|]

combineIntoCsv twainWeights columns "charFreqTwain-combined.csv"

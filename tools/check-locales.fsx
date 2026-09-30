// tools/check-locales.fsx — audits Content/Locales/*.json against en.json, the markup
// conventions and the bitmap fonts the game actually renders with.
//
// What it reports per locale: keys missing vs en, stale extras (in the locale but not
// in en), values still identical to en (split into trivial short/symbol strings and
// real untranslated leftovers), markup tokens present in en but dropped ([cl ..],
// [dl], %%, ##), and non-ASCII characters the bitmap fonts cannot render. It also
// verifies every locale file is reachable from the in-game language menu and notes
// which ones Steam auto-detection knows about.
//
// usage: dotnet fsi tools/check-locales.fsx [--strict]
//
// The default run only reports (exit 0). --strict exits 1 while any non-excluded gap
// remains, for CI. Exclusions live in `strictExclusions` below and are triaged during
// translation work — painting proper nouns (PICO-8, Totemori, ...) belong there, not
// in the locale files.

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open System.Xml.Linq

let strict = fsi.CommandLineArgs |> Array.skip 1 |> Array.contains "--strict"

let root = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))
let localesDir = Path.Combine(root, "Content", "Locales")
let fontFiles =
    [ Path.Combine(root, "Content", "Fonts", "small_font.fnt")
      Path.Combine(root, "Content", "Fonts", "large_font.fnt") ]

// Keys that are deliberately absent or English by policy, so --strict ignores them.
// Triage more here as translation work lands (painting proper nouns especially).
let strictExclusions : Set<string> =
    set
        [ // maanex thinking sounds: universal across languages
          "maanex_0"
          "maanex_1"
          "maanex_2"
          "maanex_4"
          // painting titles that are proper nouns, puns or gibberish: kept in English
          "painting_balbo"
          "painting_coce"
          "painting_kobra_throne"
          "painting_liko"
          "painting_mahula"
          "painting_mori"
          "painting_nat"
          "painting_ne_furdje_le"
          "painting_new"
          "painting_olpi"
          "painting_pico"
          "painting_qrilin"
          "painting_raj"
          "painting_tofulama"
          // painting-title exclusions above; universal tokens below
          "painting_totemori"
          "painting_totemori_redux"
          // localhost joke and proper-noun game term: identical in every language
          "bk:mask_desc"
          "happening_bk:sudoku"
          // proper-noun titles and universal tokens/emotes/tags
          "painting_banana"
          "painting_beet_boys"
          "painting_ducktective"
          "painting_egor"
          "painting_grannylisa"
          "painting_horatio"
          "painting_trasevol"
          "painting_zweihandler"
          "run_twitch"
          "seeded"
          "twitch"
          "twitch_0"
          "twitch_4"
          // ABBA lyric and UI term identical across languages
          "bk:gold_lamp_desc"
          "normal"
          "total_votes" ]

let readMap (path: string) =
    let text = File.ReadAllText(path).TrimStart('\uFEFF')
    use doc = JsonDocument.Parse(text)

    doc.RootElement.EnumerateObject()
    |> Seq.choose (fun p -> if p.Value.ValueKind = JsonValueKind.String then Some(p.Name, p.Value.GetString()) else None)
    |> Map.ofSeq

let fontGlyphs =
    fontFiles
    |> Seq.collect (fun f ->
        XDocument.Load(f).Descendants(XName.Get "char")
        |> Seq.choose (fun e ->
            match e.Attribute(XName.Get "id") with
            | null -> None
            | a -> Some(Char.ConvertFromUtf32(Int32.Parse a.Value))))
    |> Set.ofSeq

let markup = Regex(@"\[cl[^\]]*\]|\[dl\]|%%|##")
let trivialSymbolChars = [| '?'; '!'; '@'; '#'; '%'; '$'; '^'; '*'; '-'; '+'; '.'; ' ' |]
let isTrivial (v: string) = v.Length <= 4 || v.Trim(trivialSymbolChars) = ""

let en = readMap (Path.Combine(localesDir, "en.json"))
let enKeys = en |> Map.keys |> Set.ofSeq

let localeFiles =
    Directory.GetFiles(localesDir, "*.json")
    |> Array.map Path.GetFileNameWithoutExtension
    |> Array.sort

// Languages offered by the in-game menu (BurningKnight/state/InGameState.cs).
let menuLanguages =
    let src = File.ReadAllText(Path.Combine(root, "BurningKnight", "state", "InGameState.cs"))
    let block = Regex(@"Languages\s*=\s*\[(.*?)\]", RegexOptions.Singleline).Match(src)

    if block.Success then
        Regex(@"""([a-z]+)""").Matches(block.Groups.[1].Value)
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Groups.[1].Value)
        |> Set.ofSeq
    else
        Set.empty

// Steam game-language names the client maps to a locale (Desktop/integration/steam/...).
let steamNames =
    let src = File.ReadAllText(Path.Combine(root, "Desktop", "integration", "steam", "SteamIntegration.cs"))

    Regex(@"case\s+""([a-z]+)""").Matches(src)
    |> Seq.cast<Match>
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Set.ofSeq

printfn "%-4s %5s %7s %5s %6s %5s %8s %5s" "loc" "keys" "missing" "extra" "same*" "triv" "markup!" "glyph!"
printfn "%s" (String.replicate 52 "-")

let mutable strictFailures = []

for loc in localeFiles do
    if loc = "en" then
        () // the baseline itself
    else
        let map = readMap (Path.Combine(localesDir, $"{loc}.json"))
        let keys = map |> Map.keys |> Set.ofSeq
        let missing = Set.difference enKeys keys |> Set.toList |> List.sort
        let extra = Set.difference keys enKeys |> Set.toList |> List.sort

        let identical =
            Set.intersect enKeys keys
            |> Set.filter (fun k -> map.[k] = en.[k])
            |> Set.toList

        let trivial, same = identical |> List.partition (fun k -> isTrivial en.[k])

        let brokenMarkup =
            Set.intersect enKeys keys
            |> Seq.choose (fun k ->
                markup.Matches(en.[k])
                |> Seq.cast<Match>
                |> Seq.map (fun m -> m.Value)
                |> Seq.tryFind (fun tok -> not (map.[k].Contains(tok, StringComparison.Ordinal)))
                |> Option.map (fun tok -> k, tok))
            |> Seq.toList

        let missingGlyphs =
            map.Values
            |> Seq.collect id
            |> Seq.filter (fun c -> c > '\u007F' && not (fontGlyphs.Contains(string c)))
            |> Set.ofSeq
            |> Set.toList
            |> List.sort

        printfn
            "%-4s %5d %7d %5d %6d %5d %8d %5d%s%s"
            loc
            keys.Count
            missing.Length
            extra.Length
            same.Length
            trivial.Length
            brokenMarkup.Length
            missingGlyphs.Length
            (if menuLanguages.Contains loc then "" else "  [no menu entry]")
            (if loc = "ua" && not (steamNames.Contains "ukrainian") then "  [no Steam mapping]" else "")

        let gapMissing = missing |> List.filter (fun k -> not (strictExclusions.Contains k))

        if strict then
            if not gapMissing.IsEmpty then
                strictFailures <- $"[{loc}] {gapMissing.Length} missing keys" :: strictFailures

            for k, tok in brokenMarkup do
                strictFailures <- $"[{loc}] {k} drops markup {tok}" :: strictFailures

            if not missingGlyphs.IsEmpty then
                let shown = missingGlyphs |> List.truncate 40 |> List.map (fun c -> $"U+{int c:X4}")

                let rest =
                    if missingGlyphs.Length > shown.Length then
                        $" (+{missingGlyphs.Length - shown.Length} more)"
                    else
                        ""

                let codes = String.concat " " shown
                strictFailures <- $"[{loc}] {missingGlyphs.Length} chars not in bitmap font: {codes}{rest}" :: strictFailures

        if not strict then
            if not missing.IsEmpty then
                printfn "  missing (%d): %s" missing.Length (String.concat " " (missing |> List.truncate 12))

            if not extra.IsEmpty then
                printfn "  stale extras (%d): %s" extra.Length (String.concat " " (extra |> List.truncate 8))

            if not same.IsEmpty then
                printfn "  untranslated (%d): %s" same.Length (String.concat " " (same |> List.truncate 8))

            for k, tok in(brokenMarkup |> List.truncate 5) do
                printfn $"  markup: {k} drops {tok} (en: {en.[k].Substring(0, Math.Min(60, en.[k].Length))})"

            if not missingGlyphs.IsEmpty then
                let show = missingGlyphs |> List.map string |> String.concat ""
                printfn $"  no glyph for: {show}"

if strict then
    if strictFailures.IsEmpty then
        printfn "strict: all locales clean"
        exit 0
    else
        printfn "strict: %d problems" strictFailures.Length
        strictFailures |> List.rev |> List.iter (printfn "  %s")
        exit 1

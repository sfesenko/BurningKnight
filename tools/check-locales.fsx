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
          "total_votes"
          // universal meme/loanword/tech terms
          "ach_bk:maanex"
          "bk:broken_bucket_desc"
          "bk:donut"
          "bk:sudoku"
          "son_0"
          "tech"
          // ---- identical-to-en keep-class (adjudicated in round3_identity) ----
          // Every key below is byte-identical to en in at least one locale on purpose:
          // emotes/kaomoji, gamer+tech tokens, proper nouns, cited titles, or a native
          // homograph (the locale's own word merely looks English). Each block says which
          // locales keep it, so a later pass can audit instead of re-filing.
          //
          // A. universal emotes / kaomoji / marker-only strings
          "ach_bk:egor_no_more_desc" // ???
          "bk:dunce_hat_desc" // @-@
          "bk:eye_desc" // @
          "bk:eyes_desc" // @ @
          "boxy_3" // ^^:wave:^^
          "duck_10_0"
          "duck_10_1"
           // 0 / 1
          "gobetta_0"
          "gobetta_1"
          "gobetta_2"
          "gobetta_3"
          "isaac_2" // :sob:
          "shopkeeper_9" // %%^^$$$^^%%
          "ach_bk:quackers"
          "ach_bk:quackers_desc"
          "duck_17_1"
          // B. gamer/tech tokens, abbreviations, notation
          "bk:led"
          "bk:mana"
          "bk:null_hat"
          "bk:rip"
          "bk:sale_coupon_desc"
          "bk:mustache_desc"
          "bk:mustache_hat_desc"
          "bk:random_bullets_desc"
          "bk:d2"
          "bk:d4"
          "bk:d6"
          "km"
          "max_hp"
          "vsync"
          "audio"
          "pixel_perfect"
          "nullptr_0"
          // C. vocalizations / interjections (may stay English; never space-split)
          "bk:maanex_head_desc"
          "bk:villager_head_desc"
          "maanex_3"
          "maanex_10"
          "charger_5"
          "vampire_0"
          "vampire_4"
          "bk:snek_desc"
           // Snack?
          // D. names / proper nouns / cited titles
          "ach_bk:van_no_gogh"
          "bk:ankh"
          "bk:arkhalis"
          "bk:batman"
          "bk:maanex"
          "bk_7" // EDWARD
          "ach_bk:ice_boss" // Let It Go (Frozen title; cn/de/it keep)
          "ach_bk:tutorial" // Guru
          "bk:snek" // meme-source snake name (cn/de/fr/pt)
          "painting_code"
          "painting_whoops"
          "painting_sushi_sushi"
          "trash_goblin_2" // ABBA lyric (it/pt)
          "bk:wings_desc" // Celine Dion lyric (de/fr)
          // E. mode / genre labels kept in English (documented house convention)
          "ach_bk:10_challenges"
          "ach_bk:20_challenges"
          "ach_bk:30_challenges"
          "ach_bk:boss_rush"
          "boss_rush"
          "run_bossrush"
          // F. the duck/quack meme family — provisional; re-open with the duck-label call
          "control_4"
          "quack"
          "duck_6_1"
          "duck_7_1"
          "duck_20"
          "bk:duck_gun_desc"
          // G. native homographs / loanwords: the value a locale ships IS its own word
          "bk:boomerang"
          "bk:hotdog"
          "bk:katana"
          "bk:fez"
          "bk:idol"
          "bk:pass"
          "bk:saturn"
          "bk:follower"
          "bk:magnet"
          "bk:hammer"
          "bk:ushanka"
          "bk:revolver"
          "bk:missile"
          "bk:grenade"
          "bk:parachute"
          "bk:halo"
          "bk:slime"
          "bk:shawarma"
          "bk:detonator"
          "bk:dagger_desc"
          "bk:glass_gun_desc"
          "bk:headshot_gun_desc"
          "bk:sword_orbital_desc"
          "bk:bill_desc"
          "bk:scourge_of_lost_desc"
          "bk:viking_hat_desc"
          "bk:cap_desc" // Cool @-@
          "pause"
          "global"
          "score"
          "restart"
          "run"
          "top"
          "seed"
          "minutes"
          "on"
          "off"
          "no"
          "gamepad"
          "tutorial"
          "vibration"
          "happening_bk:confused"
          "happening_bk:rage"
          "happening_bk:regular_tp"
          "twitch_1_1"
          "twitch_5_1"
          "painting_null"
          // residuals adjudicated as deliberate keeps
          "bk:gamepad"     // de/fr/it loanword; pl "Pad", pt "Controle"
          "bk:marshmallow" // de/fr/it/pt loanword + the en X_X kaomoji
          "mob_0"          // "Hmmmm" is a sound, not a word
          // it keeps the loanword "Dungeon" (valid in Italian games)
          // loading-screen jokes/tips/biome titles: localised through Locale.Get
          // with en.json as the fallback (Phase 1 wiring). Locales translate
          // them gradually; missing here means English, which is current behaviour.
          "loading_joke_0"
          "loading_joke_1"
          "loading_joke_2"
          "loading_joke_3"
          "loading_joke_4"
          "loading_joke_5"
          "loading_joke_6"
          "loading_joke_7"
          "loading_joke_8"
          "loading_joke_9"
          "loading_joke_10"
          "loading_joke_11"
          "loading_joke_12"
          "loading_joke_13"
          "loading_joke_14"
          "loading_joke_15"
          "loading_joke_16"
          "loading_joke_17"
          "loading_joke_18"
          "loading_joke_19"
          "loading_joke_20"
          "loading_joke_21"
          "loading_joke_22"
          "loading_joke_23"
          "loading_joke_24"
          "loading_joke_25"
          "loading_joke_26"
          "loading_joke_27"
          "loading_joke_28"
          "loading_joke_29"
          "loading_joke_30"
          "loading_joke_31"
          "loading_joke_32"
          "loading_joke_33"
          "loading_joke_34"
          "loading_joke_35"
          "loading_joke_36"
          "loading_joke_37"
          "loading_joke_38"
          "loading_joke_39"
          "loading_joke_40"
          "loading_joke_41"
          "loading_joke_42"
          "loading_joke_43"
          "loading_joke_44"
          "loading_joke_45"
          "loading_joke_46"
          "loading_joke_47"
          "loading_joke_48"
          "loading_joke_49"
          "loading_joke_50"
          "loading_joke_51"
          "loading_joke_52"
          "loading_joke_53"
          "loading_joke_54"
          "loading_joke_55"
          "loading_joke_56"
          "loading_joke_57"
          "loading_joke_58"
          "loading_joke_59"
          "loading_joke_60"
          "loading_joke_61"
          "loading_joke_62"
          "loading_joke_63"
          "loading_joke_64"
          "loading_joke_65"
          "loading_joke_66"
          "loading_joke_67"
          "loading_joke_68"
          "loading_joke_69"
          "loading_joke_70"
          "loading_joke_71"
          "loading_joke_72"
          "loading_joke_73"
          "loading_joke_74"
          "loading_joke_75"
          "loading_joke_76"
          "loading_joke_77"
          "loading_tip_0"
          "loading_tip_1"
          "loading_tip_2"
          "loading_tip_3"
          "loading_tip_4"
          "loading_tip_5"
          "loading_biome_hub_0"
          "loading_biome_hub_1"
          "loading_biome_castle_0"
          "loading_biome_castle_1"
          "loading_biome_desert_0"
          "loading_biome_desert_1"
          "loading_biome_desert_2"
          "loading_biome_desert_3"
          "loading_biome_jungle_0"
          "loading_biome_jungle_1"
          "loading_biome_jungle_2"
          "loading_biome_jungle_3"
          "loading_biome_ice_0"
          "loading_biome_ice_1"
          "loading_biome_ice_2"
          "loading_biome_ice_3"
          "loading_biome_ice_4"
          "loading_biome_ice_5"
          "loading_biome_ice_6"
          "loading_biome_library_0"
          "loading_biome_library_1"
          "loading_biome_library_2"
          "loading_biome_library_3"
          "loading_biome_library_4"
          "loading_biome_library_5"
          "loading_biome_tech_0"
          "loading_biome_tech_1"
          "loading_biome_tech_2"
          "loading_biome_tech_3"
          "loading_biome_tech_4"
          "loading_biome_tech_5"
          "loading_biome_tech_6"
          "loading_biome_cave_0"
          "loading_biome_cave_1"
          "loading_biome_cave_2"
          "loading_biome_cave_3"
          "loading_biome_cave_4"
          "painting_dungeon" ]

// The game parses locales with Lens/lightJson (JsonReader), which throws on
// duplicate object keys, and Locale.Load installs an empty map *before* parsing —
// so a single duplicate key silently drops the whole language back to English.
// System.Text.Json instead keeps the last value, so the duplicate has to be
// detected here: the checker's own parser would never see it.
let readMapRaw (path: string) =
    let text = File.ReadAllText(path).TrimStart('\uFEFF')
    use doc = JsonDocument.Parse(text)
    let pairs, dups = ResizeArray(), ResizeArray()

    doc.RootElement.EnumerateObject()
    |> Seq.iter (fun p ->
        if pairs |> Seq.exists (fun (n, _) -> n = p.Name) then
            dups.Add(p.Name)

        pairs.Add(p.Name, p.Value))

    pairs |> Seq.choose (fun (n, v) -> if v.ValueKind = JsonValueKind.String then Some(n, v.GetString()) else None) |> Map.ofSeq,
    dups |> Seq.distinct |> List.ofSeq

let readMap path = fst (readMapRaw path)

// Per-file glyph sets, plus the union: the game loads every page of both fonts into
// one flat font (BurningKnight/assets/Font.cs), so the union is what can render.
// MonoGame.Extended has no .notdef fallback, so an uncovered char draws nothing at
// all — this gate is the only signal for unrenderable text.
let fontGlyphsPerFile =
    fontFiles
    |> List.toArray
    |> Array.map (fun f ->
        XDocument.Load(f).Descendants(XName.Get "char")
        |> Seq.choose (fun e ->
            match e.Attribute(XName.Get "id") with
            | null -> None
            | a -> Some(string (Char.ConvertFromUtf32(Int32.Parse a.Value))))
        |> Set.ofSeq)

let fontGlyphs = fontGlyphsPerFile |> Array.fold Set.union Set.empty

// All markup the parser understands (UiString.Recalculate): colour, delay, wave,
// blink, randomiser, emphasis and italic. A token present in en must survive in
// the locale, and the pair counts must match (the parser only treats adjacent
// pairs as markers, and a lone `_` silently deletes itself).
let markupTokens = [| "[cl"; "[dl]"; "[ic"; "[vr"; "^^"; "@@"; "%%"; "##"; "~~" |]
let markup = Regex(@"\[cl[^\]]*\]|\[dl\]|%%|##")
let tokenCount (v: string) (tok: string) =
    if tok = "[cl" then
        Regex(@"\[cl[^\]]*\]").Matches(v).Count
    elif tok = "[ic" || tok = "[vr" then
        Regex(Regex.Escape(tok + " ")).Matches(v).Count
    else
        Regex(Regex.Escape(tok)).Matches(v).Count

// A locale may add emphasis the English source does not have (many do, and that is
// fine); what it may not do is lose or split a token. The comparison is one-sided:
// fewer tokens than en is a failure, more is only reported.
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

// Font.Small and Font.Large are separate atlases but one shared coverage set: a
// char baked into only one of them renders in some UI and silently vanishes in
// another, which the union-based per-locale check cannot see.
let smallOnly = Set.difference fontGlyphsPerFile.[0] fontGlyphsPerFile.[1]
let largeOnly = Set.difference fontGlyphsPerFile.[1] fontGlyphsPerFile.[0]

let fmtChars (s: Set<string>) =
    let one (c: string) =
        let n = Char.ConvertToUtf32(c, 0)
        if n > 0x7F then sprintf "U+%04X " n else sprintf "'%s' " c

    s |> Seq.truncate 20 |> Seq.map one |> String.concat ""

if not smallOnly.IsEmpty then
    strictFailures <- $"[fonts] {smallOnly.Count} chars in small_font.fnt but not large_font.fnt: {fmtChars smallOnly}" :: strictFailures

if not largeOnly.IsEmpty then
    strictFailures <- $"[fonts] {largeOnly.Count} chars in large_font.fnt but not small_font.fnt: {fmtChars largeOnly}" :: strictFailures

for loc in localeFiles do
    let map, dupKeys = readMapRaw (Path.Combine(localesDir, $"{loc}.json"))
    let keys = map |> Map.keys |> Set.ofSeq
    let missing = Set.difference enKeys keys |> Set.toList |> List.sort
    let extra = Set.difference keys enKeys |> Set.toList |> List.sort

    // Empty/whitespace values render as a blank node; the game never validates them.
    let emptyValues =
        map
        |> Map.toList
        |> List.choose (fun (k, v) -> if String.IsNullOrWhiteSpace(v) then Some k else None)

    let joinKeys xs = String.concat " " (xs |> List.truncate 12)

    if not dupKeys.IsEmpty then
        strictFailures <- $"[{loc}] duplicate keys (the game throws and drops the language): {joinKeys dupKeys}" :: strictFailures

    if not emptyValues.IsEmpty then
        strictFailures <- $"[{loc}] {emptyValues.Length} empty values: {joinKeys emptyValues}" :: strictFailures

    if loc <> "en" then
        if not extra.IsEmpty then
            strictFailures <- $"[{loc}] {extra.Length} keys not in en (stale): {joinKeys extra}" :: strictFailures

        // Deliberate keeps (strictExclusions) are subtracted here too, not just from the
        // missing-key check: a value that is byte-identical to en on purpose must not be
        // re-reported by every future pass (plan item T0.4).
        let identical =
            Set.intersect enKeys keys
            |> Set.filter (fun k -> map.[k] = en.[k])
            |> Set.filter (fun k -> not (strictExclusions.Contains k))
            |> Set.toList

        let trivial, same = identical |> List.partition (fun k -> isTrivial en.[k])

        // Every markup token must survive with the same count as in en: presence-only
        // matching let `##A##` -> `#A##` through, and the parser only honours adjacent
        // pairs, so a half-token changes how the string renders.
        let brokenMarkup =
            Set.intersect enKeys keys
            |> Seq.collect (fun k ->
                markupTokens
                |> Seq.choose (fun tok ->
                    if tokenCount map.[k] tok < tokenCount en.[k] tok then
                        Some(k, tok, tokenCount en.[k] tok, tokenCount map.[k] tok)
                    else
                        None))
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

            // Untranslated leftovers are a strict failure: `same` was computed and
            // printed but never enforced, so reverting a locale to English passed
            // silently. Trivial strings stay report-only (they are often correct).
            if not same.IsEmpty then
                strictFailures <-
                    $"[{loc}] {same.Length} untranslated (identical to en): {joinKeys same}"
                    :: strictFailures

            for k, tok, enN, locN in brokenMarkup do
                strictFailures <- $"[{loc}] {k} loses markup {tok} ({enN}->{locN})" :: strictFailures

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

            for k, tok, enN, locN in(brokenMarkup |> List.truncate 8) do
                printfn $"  markup: {k} loses {tok} ({enN}->{locN}) (en: {en.[k].Substring(0, Math.Min(60, en.[k].Length))})"

            if not dupKeys.IsEmpty then
                printfn "  duplicate keys (%d): %s" dupKeys.Length (joinKeys dupKeys)

            if not emptyValues.IsEmpty then
                printfn "  empty values (%d): %s" emptyValues.Length (joinKeys emptyValues)

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

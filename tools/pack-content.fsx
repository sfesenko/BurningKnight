// tools/pack-content.fsx — packs the runtime content into one archive, compressing the sound
// effects on the way when ffmpeg is around.
//
// The archive holds only what the game reads: sources (.ase, .fx, .ttf), the content project's
// build output and files nothing loads stay out. Everything is deflated — measured, the entry
// streams are not seekable either way, and SoundEffect.FromStream reads them all.
//
// Conversion is incremental, keyed on the source's timestamp, and remembers which codec produced
// the audio in bin/Pack/sfx.mode — so switching ffmpeg on or off rebuilds it instead of silently
// keeping the old format.
//
// usage: dotnet fsi tools/pack-content.fsx <contentDir> <outputZip>

open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.IO.Compression

let args = fsi.CommandLineArgs |> Array.skip 1

if args.Length <> 2 then
    printfn "usage: dotnet fsi tools/pack-content.fsx <contentDir> <outputZip>"
    exit 1

let content = Path.GetFullPath args.[0]
let output = Path.GetFullPath args.[1]
let staging = Path.Combine(content, "bin", "Pack", "Sfx")
let modeFile = Path.Combine(content, "bin", "Pack", "sfx.mode")

let relative (path: string) =
    Path.GetRelativePath(content, path).Replace('\\', '/')

// The payload, by its path in the archive. Explicit on purpose: a file the runtime does not read
// must not ride along, and a new asset class is a deliberate line here.
let isPayload (rel: string) =
    let ext = Path.GetExtension(rel).ToLowerInvariant()
    let under (dir: string) = rel.StartsWith(dir + "/", StringComparison.Ordinal)

    match rel with
    | "items.json" | "achievements.json" -> true
    | _ when not (rel.Contains '/') && ext = ".png" -> true
    | _ when under "Textures" -> ext = ".png"
    // Every .ttf is a font source for the game's own fonts, which the runtime reads as .fnt
    // plus .png; the overlay's font is a development tool and never loads in a release.
    | _ when under "Fonts" -> ext = ".fnt" || ext = ".png"
    | _ when under "Locales" || under "Dialogs" -> ext = ".json"
    | _ when under "Prefabs" -> ext = ".lvl"
    | _ when under "Sfx" -> ext = ".wav"
    // Music ships in the archive too; the runtime copies a track out on first play, because a
    // Song opens from a path and not from a stream.
    | _ when under "Music" -> ext = ".ogg"
    | _ when under "bin/Animations" -> true
    | _ when under "bin/Shaders" -> ext = ".xnb"
    | _ -> false

// Generated content loses the bin/ prefix: the archive holds what the runtime asks for.
let entryName (rel: string) =
    if rel.StartsWith("bin/", StringComparison.Ordinal) then rel.Substring 4 else rel

let payload =
    Directory.EnumerateFiles(content, "*", SearchOption.AllDirectories)
    |> Seq.filter (fun path -> isPayload (relative path))
    |> Seq.sortBy relative
    |> List.ofSeq

if not (payload |> List.exists (fun path -> relative path = "bin/Animations/animations.json")) then
    printfn "error: bin/Animations/animations.json is missing — the preprocessor has not run"
    exit 1

// --- sound effects ------------------------------------------------------------------------------

let ffmpeg = "ffmpeg"
let codecs = [ "adpcm_ima_wav"; "adpcm_ms" ]

let run (arguments: string list) =
    let info = ProcessStartInfo(ffmpeg)
    info.UseShellExecute <- false
    info.RedirectStandardOutput <- true
    info.RedirectStandardError <- true
    arguments |> List.iter (fun argument -> info.ArgumentList.Add argument)

    try
        use ffmpegProcess = Process.Start info
        // Both pipes are drained: a full one would block the tool.
        ffmpegProcess.StandardError.ReadToEnd() |> ignore
        ffmpegProcess.StandardOutput.ReadToEnd() |> ignore
        ffmpegProcess.WaitForExit()
        ffmpegProcess.ExitCode
    with :? Win32Exception ->
        // ffmpeg is not on PATH.
        -1

let mode =
    if run [ "-version" ] = 0 then codecs.[0] else "pcm"

Directory.CreateDirectory staging |> ignore

if (if File.Exists modeFile then File.ReadAllText modeFile else "") <> mode then
    for staged in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories) do
        File.Delete staged

    File.WriteAllText(modeFile, mode)

let convert (source: string) (target: string) =
    Directory.CreateDirectory(Path.GetDirectoryName target) |> ignore

    let tryCodec (codec: string) =
        if File.Exists target then File.Delete target

        let code = run [ "-y"; "-loglevel"; "error"; "-i"; source; "-c:a"; codec; target ]
        code = 0 && File.Exists target && FileInfo(target).Length > 0

    let mutable produced = None

    for codec in codecs do
        if produced.IsNone && tryCodec codec then
            produced <- Some codec

    if produced.IsNone && File.Exists target then
        File.Delete target

    produced

let sfx =
    payload |> List.filter (fun path -> (relative path).StartsWith("Sfx/", StringComparison.Ordinal))

let mutable converted = 0
let mutable copied = 0
let mutable unchanged = 0

for source in sfx do
    let target = Path.Combine(staging, (relative source).Substring 4)

    if File.Exists target && File.GetLastWriteTimeUtc target > File.GetLastWriteTimeUtc source then
        unchanged <- unchanged + 1
    elif mode = "pcm" then
        Directory.CreateDirectory(Path.GetDirectoryName target) |> ignore
        File.Copy(source, target, true)
        copied <- copied + 1
    else
        match convert source target with
        | Some _ -> converted <- converted + 1
        | None ->
            printfn "WARNING: could not compress %s, copying it as PCM" (relative source)
            Directory.CreateDirectory(Path.GetDirectoryName target) |> ignore
            File.Copy(source, target, true)
            copied <- copied + 1

// A sound removed from the content must not stay in the archive.
let staged =
    sfx |> List.map (fun source -> Path.Combine(staging, (relative source).Substring 4)) |> Set.ofList

for file in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories) do
    if not (staged.Contains file) then
        File.Delete file

if mode = "pcm" then
    printfn ""
    printfn "WARNING: ffmpeg was not found on PATH, so the sound effects ship as PCM and the archive is much larger than it needs to be. Install ffmpeg to shrink it."

// --- the archive --------------------------------------------------------------------------------

Directory.CreateDirectory(Path.GetDirectoryName output) |> ignore

if File.Exists output then
    File.Delete output

let entries =
    payload
    |> List.map (fun source ->
        let rel = relative source

        if rel.StartsWith("Sfx/", StringComparison.Ordinal)
        then source, Path.Combine(staging, rel.Substring 4), rel
        else source, source, rel)

let writeArchive () =
    use stream = File.Create output
    use zip = new ZipArchive(stream, ZipArchiveMode.Create)

    for _, file, rel in entries do
        let entry = zip.CreateEntry(entryName rel, CompressionLevel.Optimal)
        entry.LastWriteTime <- File.GetLastWriteTime file

        use target = entry.Open()
        use input = File.OpenRead file

        input.CopyTo target

writeArchive ()

// --- what happened ------------------------------------------------------------------------------

let megabytes (bytes: int64) = float bytes / 1e6

let packed = entries |> List.sumBy (fun (_, file, _) -> FileInfo(file).Length)
let sources = sfx |> List.sumBy (fun source -> FileInfo(source).Length)
let shipped = entries
              |> List.filter (fun (_, _, rel) -> rel.StartsWith("Sfx/", StringComparison.Ordinal))
              |> List.sumBy (fun (_, file, _) -> FileInfo(file).Length)

printfn ""
printfn "packed %d files, %.1f MB -> %s (%.1f MB)" entries.Length (megabytes packed) output (megabytes (FileInfo(output).Length))
printfn "sfx %d files: %.1f MB PCM -> %.1f MB (%s)" sfx.Length (megabytes sources) (megabytes shipped) mode

if unchanged > 0 then
    printfn "    %d unchanged, %d converted, %d copied" unchanged converted copied

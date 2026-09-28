// tools/preprocess-ase.fsx — turns Content/Animations/*.ase into one PNG per animation plus a
// single animations.json, at build time. No MGCB, no Aseprite install.
//
// Two quirks are load-bearing and reproduced deliberately, because the runtime can read them:
//   * bands are indexed by *cel index*, not layer index — a layerless animation draws the first
//     layer, so the gobbo particle depends on it;
//   * the atlas is one row taller than the bands and the blit is unclipped, so an off-canvas cel
//     wraps into the next row (worm) or spills into the next band (gobbo), exactly as today.
//
// The pixel comparison at the bottom loads the old first-party parser, so running this needs a
// Debug build of Aseprite/ and Desktop/; delete that block together with Aseprite/ (WS-1 T1.5).
//
// Input that would silently produce wrong pixels fails the run: cels out of layer order, an atlas
// past the texture cap, a file with no frames or layers.
#r "nuget: AsepriteDotNet, 1.9.1"
#r "../Aseprite/bin/Debug/net10.0/Aseprite.dll"
#r "../Desktop/bin/Debug/net10.0/MonoGame.Framework.dll"

open System
open System.Buffers.Binary
open System.IO
open System.IO.Compression
open System.Text.Json
open System.Text.Json.Nodes
open AsepriteDotNet.IO
open AsepriteDotNet.Aseprite
open AsepriteDotNet.Aseprite.Types

let root = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))
let content = Path.Combine(root, "BurningKnight", "Content")
let source = Path.Combine(content, "Animations")
let output = Path.Combine(content, "bin", "Animations")

Directory.CreateDirectory output |> ignore

// --- PNG: IHDR + IDAT (zlib over filter-0 scanlines) + IEND ----------------------------------

let crcTable =
    Array.init 256 (fun n ->
        let mutable c = uint32 n

        for _ in 1 .. 8 do
            c <- if c &&& 1u = 1u then 0xEDB88320u ^^^ (c >>> 1) else c >>> 1

        c)

let crc32 (data: byte[]) =
    let mutable c = 0xFFFFFFFFu

    for b in data do
        c <- crcTable.[int ((c ^^^ uint32 b) &&& 0xFFu)] ^^^ (c >>> 8)

    c ^^^ 0xFFFFFFFFu

let writePng (path: string) (width: int) (height: int) (rgba: byte[]) =
    let stride = width * 4
    let raw = Array.zeroCreate ((stride + 1) * height)

    for y in 0 .. height - 1 do
        raw.[y * (stride + 1)] <- 0uy
        Buffer.BlockCopy(rgba, y * stride, raw, y * (stride + 1) + 1, stride)

    use file = File.Create path

    let writeBytes (data: byte[]) = file.Write(data, 0, data.Length)

    writeBytes [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

    let chunk (name: string) (data: byte[]) =
        let length = data.Length
        writeBytes [| byte (length >>> 24); byte (length >>> 16); byte (length >>> 8); byte length |]

        let name = Text.Encoding.ASCII.GetBytes name
        writeBytes name
        writeBytes data

        let crc = crc32 (Array.append name data)
        writeBytes [| byte (crc >>> 24); byte (crc >>> 16); byte (crc >>> 8); byte crc |]

    let header = Array.zeroCreate 13
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan 0, width)
    BinaryPrimitives.WriteInt32BigEndian(header.AsSpan 4, height)
    header.[8] <- 8uy // bit depth
    header.[9] <- 6uy // RGBA
    chunk "IHDR" header

    let compressed =
        use buffer = new MemoryStream()
        let zlib = new ZLibStream(buffer, CompressionLevel.Optimal, true)
        zlib.Write(raw, 0, raw.Length)
        zlib.Dispose()
        buffer.ToArray()

    chunk "IDAT" compressed
    chunk "IEND" [||]

// --- compose ---------------------------------------------------------------------------------

let rec celImage (cel: AsepriteCel) =
    match cel with
    | :? AsepriteImageCel as image -> image.Pixels.ToArray(), image.Size.Width, image.Size.Height
    | :? AsepriteLinkedCel as linked -> celImage linked.Cel
    | _ -> failwithf "unsupported cel type %s" (cel.GetType().Name)

let mutable missingCels = 0
let mutable differences = 0
let animations = JsonObject()

// GraphicsProfile.Reach, the profile the game targets.
let maxTextureSize = 2048

for path in Directory.GetFiles(source, "*.ase") |> Array.sort do
    let name = Path.GetFileNameWithoutExtension path
    let file =
        try
            AsepriteFileLoader.FromFile(path, false)
        with ex ->
            failwithf "cannot read %s: %s" name ex.Message

    let frames = file.Frames.ToArray()
    let layers = file.Layers.ToArray()

    if frames.Length = 0 || layers.Length = 0 then
        failwithf "%s has no %s" name (if frames.Length = 0 then "frames" else "layers")

    // The old parser premultiplied RGBA and grayscale pixels but left indexed palette entries as
    // they are; reproduce that exactly, so the flag stays off and the multiplication is manual.
    let premultiply = file.ColorDepth <> AsepriteColorDepth.Indexed
    let width = file.CanvasWidth
    let height = file.CanvasHeight
    let atlasWidth = frames.Length * width
    // One row taller than the bands, exactly like the old buffer: its flat indexing lets an
    // off-canvas cel wrap into the next row (visible in worm) and the last band's overflow land in
    // this padding row, which the runtime never samples.
    let atlasHeight = layers.Length * height + 1

    if atlasWidth > maxTextureSize || atlasHeight > maxTextureSize then
        failwithf "%s: the %dx%d atlas exceeds the %d texture cap" name atlasWidth atlasHeight maxTextureSize

    let rgba = Array.zeroCreate (atlasWidth * atlasHeight * 4)

    for f in 0 .. frames.Length - 1 do
        let cels = frames.[f].Cels.ToArray()

        if cels.Length < layers.Length then
            missingCels <- missingCels + 1
            printfn "missing cel: %s frame %s — %d cels for %d layers (banded by cel index, like the old blit)" name frames.[f].Name cels.Length layers.Length

        for celNo in 0 .. cels.Length - 1 do
            let cel = cels.[celNo]

            if cel.Layer.Name <> layers.[celNo].Name then
                failwithf "%s frame %s: cel %d belongs to layer '%s', but band %d is '%s'" name frames.[f].Name celNo cel.Layer.Name celNo layers.[celNo].Name

            let pixels, celWidth, celHeight = celImage cel
            let x = cel.Location.X
            let y = cel.Location.Y

            for cy in 0 .. celHeight - 1 do
                for cx in 0 .. celWidth - 1 do
                    let pixel = pixels.[cx + cy * celWidth]
                    let o = ((celNo * height + y + cy) * atlasWidth + f * width + x + cx) * 4

                    if premultiply then
                        let alpha = int pixel.A
                        rgba.[o] <- byte (int pixel.R * alpha / 255)
                        rgba.[o + 1] <- byte (int pixel.G * alpha / 255)
                        rgba.[o + 2] <- byte (int pixel.B * alpha / 255)
                    else
                        rgba.[o] <- pixel.R
                        rgba.[o + 1] <- pixel.G
                        rgba.[o + 2] <- pixel.B

                    rgba.[o + 3] <- pixel.A

    writePng (Path.Combine(output, name + ".png")) atlasWidth atlasHeight rgba

    let entry = JsonObject()
    entry.["width"] <- width
    entry.["height"] <- height
    entry.["frames"] <- frames.Length
    entry.["layers"] <- JsonArray(Array.ofSeq (layers |> Array.map (fun l -> JsonValue.Create l.Name :> JsonNode)))

    let durations = JsonArray()

    for frame in frames do
        durations.Add(JsonValue.Create frame.Duration.TotalSeconds)

    entry.["durations"] <- durations

    let tags = JsonObject()

    for tag in file.Tags.ToArray() do
        let t = JsonObject()
        t.["from"] <- tag.From
        t.["to"] <- tag.To
        t.["direction"] <- int tag.LoopDirection
        tags.[tag.Name] <- t

    entry.["tags"] <- tags

    let slices = JsonObject()

    for slice in file.Slices.ToArray() do
        // the runtime keeps the last key of a repeated name
        let key = slice.Keys.ToArray() |> Array.maxBy (fun k -> k.FrameIndex)
        let s = JsonObject()
        s.["x"] <- key.Bounds.X
        s.["y"] <- key.Bounds.Y
        s.["width"] <- key.Bounds.Width
        s.["height"] <- key.Bounds.Height
        slices.[slice.Name] <- s

    entry.["slices"] <- slices
    animations.[name] <- entry

    // --- verify against the old parser, while it is still here ------------------------------

    let old = Aseprite.AsepriteFile.ReadAsepriteFile path
    let expected = old.PixelData
    let oldWidth = old.TextureWidth

    if oldWidth <> atlasWidth || old.TextureHeight + 1 <> atlasHeight then
        differences <- differences + 1
        printfn "size mismatch: %s — old %dx%d vs new %dx%d" name oldWidth (old.TextureHeight + 1) atlasWidth atlasHeight
    else
        for y in 0 .. atlasHeight - 1 do
            for x in 0 .. atlasWidth - 1 do
                let pixel = expected.[y * oldWidth + x]
                let o = (y * atlasWidth + x) * 4

                if pixel.R <> rgba.[o] || pixel.G <> rgba.[o + 1] || pixel.B <> rgba.[o + 2] || pixel.A <> rgba.[o + 3] then
                    differences <- differences + 1
                    printfn "pixel mismatch: %s at %d,%d" name x y

                    if differences > 10 then
                        failwith "too many pixel differences"

let document = JsonObject()
document.["version"] <- 1
document.["animations"] <- animations
File.WriteAllText(Path.Combine(output, "animations.json"), document.ToJsonString(JsonSerializerOptions(WriteIndented = true)))

printfn ""
printfn "wrote %d PNGs and animations.json to %s" animations.Count output
printfn "missing-cel frames %d, pixel differences %d" missingCels differences

if differences > 0 then
    failwithf "%d pixel differences against the old parser" differences

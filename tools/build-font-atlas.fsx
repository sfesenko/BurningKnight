// tools/build-font-atlas.fsx — extends the game's bitmap fonts with the glyphs the
// locale files need but the atlases lack (CJK for cn, Polish diacritics, Belarusian
// ў/Ў, NBSP, CJK punctuation for translators).
//
// The game renders everything through Content/Fonts/small_font and large_font (.fnt
// descriptor plus one PNG page each) and never touches a TTF at runtime, so the fix
// is offline: new glyphs are rasterized here from Content/Fonts/fnt.ttf (indienova,
// CJK) and Content/Fonts/font.ttf (Roboto, everything else) and appended as a second
// page. No .cs changes; the release pack already ships Fonts/*.png.
//
// The existing atlases are 1-bit white core plus a baked 1px black halo; new glyphs
// replicate that exactly, sit on the same baseline (small base=12, large base=13),
// and Roboto glyphs are scaled to the SinsGold cap height so mixed words look whole.
// The run is idempotent: pages past id 0 are stripped and rebuilt every time, and
// anything that would silently produce wrong pixels fails the run. The originals are
// derived artifacts too — git restores them.
//
// usage: dotnet fsi tools/build-font-atlas.fsx
#r "nuget: SkiaSharp, 3.116.1"
#r "nuget: SkiaSharp.NativeAssets.Linux, 3.116.1"

open System
open System.IO
open System.Linq
open System.Text.RegularExpressions
open System.Xml.Linq
open SkiaSharp

let root = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))
let content = Path.Combine(root, "Content")
let localesDir = Path.Combine(content, "Locales")
let fontsDir = Path.Combine(content, "Fonts")
let previewDir = "/tmp/opencode"

let xn s = XName.Get s

// Chars translators will need that no locale file uses yet: Polish diacritics,
// Belarusian ў/Ў, quotes/dashes and CJK punctuation. Anything already in the font
// or unrenderable is filtered below, so this list can stay generous.
let extraChars = "ąćęłńóśźżĄĆĘŁŃÓŚŹŻўЎ –—«»…·　、。？！：；「」『』〈〉《》【】（）" + " –—„\"\"«»…·"

type FontSpec =
    { Fnt: string
      ExtPage: string
      LatinFace: string
      LineHeight: int
      Base: int }

let fonts =
    [ { Fnt = "small_font.fnt"
        ExtPage = "small_ext.png"
        LatinFace = "SinsGold"
        LineHeight = 15
        Base = 12 }
      { Fnt = "large_font.fnt"
        ExtPage = "large_ext.png"
        LatinFace = "CompassGold"
        LineHeight = 17
        Base = 13 } ]

let fail msg = failwith $"build-font-atlas: {msg}"

let loadFnt (spec: FontSpec) =
    let doc = XDocument.Load(Path.Combine(fontsDir, spec.Fnt))
    let chars = doc.Descendants(xn "char") |> Seq.toArray
    let ids = chars |> Seq.choose (fun e -> e.Attribute(xn "id") |> Option.ofObj |> Option.map (fun a -> Int32.Parse a.Value)) |> Set.ofSeq
    doc, chars, ids

// Every non-ASCII char the translations use, minus what the font already has.
let inventory (existing: Set<int>) =
    let used =
        Directory.GetFiles(localesDir, "*.json")
        |> Seq.filter (fun f -> Path.GetFileNameWithoutExtension f <> "qu")
        |> Seq.collect (fun f -> File.ReadAllText(f).TrimStart('\uFEFF'))
        |> Seq.filter (fun c -> c > '\u007F')
        |> Set.ofSeq

    let extras = extraChars |> Seq.filter (fun c -> c > '\u007F') |> Set.ofSeq
    Set.union used extras
    |> Set.filter (fun c ->
        let cp = Char.ConvertToUtf32(string c, 0)
        cp <= 0xFFFF && not (existing.Contains cp))
    |> Set.toList
    |> List.sort

let indie = SKTypeface.FromFile(Path.Combine(fontsDir, "fnt.ttf"))
let roboto = SKTypeface.FromFile(Path.Combine(fontsDir, "font.ttf"))

if isNull (box indie) then fail "could not load fnt.ttf"
if isNull (box roboto) then fail "could not load font.ttf"

// True when the face really has the glyph (not .notdef).
let hasGlyph (face: SKTypeface) (c: char) = face.GetGlyph(int c) <> 0us

type Cell =
    { Char: char
      Pixels: bool[,]
      XOffset: int
      YOffset: int
      XAdvance: int }

// None when the char has no ink (spacing chars) — the caller blanks those.
// `threshold` is the alpha cutoff for the 1-bit core: higher = lighter strokes.
let rasterize (face: SKTypeface) (size: float32) (scale: float32) (baseLine: int) (threshold: byte) (c: char) =
    use font = new SKFont(face, size * scale)
    font.Hinting <- SKFontHinting.Full
    font.Edging <- SKFontEdging.Antialias
    use paint = new SKPaint(Color = SKColors.White)

    let advance = font.MeasureText(string c)
    use path = font.GetTextPath(string c, SKPoint.Empty)
    let b = path.Bounds

    if b.Width <= 0f || b.Height <= 0f then
        None
    else
        // Draw with the baseline at a known spot in a scratch bitmap.
        let ox = 8
        let oy = 64
        let w = int (Math.Ceiling(float (advance + 16f)))
        let h = 128

        use bmp = new SKBitmap(w, h)
        use canvas = new SKCanvas(bmp)
        canvas.DrawText(string c, float32 ox, float32 oy, SKTextAlign.Left, font, paint)
        canvas.Flush()

        let alpha x y = if x < 0 || y < 0 || x >= w || y >= h then 0uy else bmp.GetPixel(x, y).Alpha

        // 1-bit mask like the original atlases, then a 1px black halo around the core.
        let mask = Array2D.init w h (fun x y -> alpha x y >= threshold)

        let mutable l, t, r, bb = w, h, -1, -1

        for y in 0 .. h - 1 do
            for x in 0 .. w - 1 do
                let mutable halo = mask.[x, y]

                if not halo then
                    for dy in -1 .. 1 do
                        for dx in -1 .. 1 do
                            let xx, yy = x + dx, y + dy

                            if xx >= 0 && yy >= 0 && xx < w && yy < h && mask.[xx, yy] then
                                halo <- true

                if halo then
                    l <- min l x
                    t <- min t y
                    r <- max r x
                    bb <- max bb y

        if r < 0 then
            None
        else
            let cw, ch = r - l + 1, bb - t + 1
            let pixels = Array2D.init cw ch (fun x y -> mask.[l + x, t + y])

            // Pen at (ox, oy), line top oy - baseLine: offsets relative to those.
            Some
                { Char = c
                  Pixels = pixels
                  XOffset = l - ox
                  YOffset = t - (oy - baseLine)
                  XAdvance = int (Math.Round(float advance)) }

// Advance of the existing space — the width for blank chars without a better source.
let spaceAdvance (chars: XElement[]) =
    chars
    |> Array.find (fun e -> e.Attribute(xn "id").Value = "32")
    |> fun sp -> Int32.Parse(sp.Attribute(xn "xadvance").Value)

// A 1x1 transparent cell (zero-size rects would make a degenerate texture region).
let blankCell c adv =
    { Char = c
      Pixels = Array2D.create 1 1 false
      XOffset = 0
      YOffset = 0
      XAdvance = adv }

let whiteCoreHeight (pngPath: string) (rect: int * int * int * int) =
    use bmp = SKBitmap.Decode pngPath
    let x, y, w, h = rect
    let mutable top, bot = h, -1

    for yy in y .. y + h - 1 do
        for xx in x .. x + w - 1 do
            let p = bmp.GetPixel(xx, yy)

            if p.Red = 255uy && p.Green = 255uy && p.Blue = 255uy && p.Alpha = 255uy then
                top <- min top (yy - y)
                bot <- max bot (yy - y)

    if bot < 0 then fail $"no white pixels in {pngPath} rect"
    bot - top + 1

let build (spec: FontSpec) =
    let fntPath = Path.Combine(fontsDir, spec.Fnt)

    // Previous extension output stays put: rebuilds only append genuinely new
    // glyphs below the existing rows, so committed diffs stay reviewable.
    let doc, chars, existing = loadFnt spec

    let kept =
        chars
        |> Array.filter (fun e -> e.Attribute(xn "page").Value = "1")
        |> Array.map (fun e ->
            (Int32.Parse(e.Attribute(xn "id").Value),
             Int32.Parse(e.Attribute(xn "x").Value),
             Int32.Parse(e.Attribute(xn "y").Value),
             Int32.Parse(e.Attribute(xn "width").Value),
             Int32.Parse(e.Attribute(xn "height").Value)))
        |> Array.toList

    let extPath = Path.Combine(fontsDir, spec.ExtPage)

    if not kept.IsEmpty && not (File.Exists extPath) then
        fail $"{spec.ExtPage} referenced by {spec.Fnt} but missing on disk"

    let needed = inventory existing

    printfn $"{spec.Fnt}: {needed.Length} glyphs to add"

    if not needed.IsEmpty then
        // White 'A' core height in the current atlas — the scale reference.
        let a =
            chars |> Array.find (fun e -> e.Attribute(xn "id").Value = "65")

        let ax = Int32.Parse(a.Attribute(xn "x").Value)
        let ay = Int32.Parse(a.Attribute(xn "y").Value)
        let aw = Int32.Parse(a.Attribute(xn "width").Value)
        let ah = Int32.Parse(a.Attribute(xn "height").Value)
        let page0 = Path.Combine(fontsDir, (doc.Descendants(xn "page") |> Seq.head).Attribute(xn "file").Value)
        let targetCap = whiteCoreHeight page0 (ax, ay, aw, ah)

        use capFont = new SKFont(roboto, float32 spec.LineHeight)
        use capPath = capFont.GetTextPath("A", SKPoint.Empty)
        let robotoScale = float32 targetCap / capPath.Bounds.Height

        printfn $"  {spec.LatinFace} cap {targetCap}px, Roboto scale {robotoScale:F2}"

        let cells =
            needed
            |> List.map (fun c ->
                let rendered =
                    if hasGlyph indie c then
                        // CJK is optically heavier than latin at the same size: render it
                        // smaller (LineHeight - 4: 11px/13px vs the 7px latin cap reference)
                        // and threshold harder, so stroke weight matches latin instead of
                        // doubling it. Baseline unchanged, so mixed lines stay aligned.
                        let size = float32 (spec.LineHeight - 4)
                        rasterize indie size 1f spec.Base 200uy c
                    elif hasGlyph roboto c then
                        rasterize roboto (float32 spec.LineHeight) robotoScale spec.Base 128uy c
                    else
                        None

                match rendered with
                | Some cell -> cell
                | None ->
                    if c = '\u3000' then
                        blankCell c spec.LineHeight // ideographic space: full em
                    elif c = '\u00A0' then
                        blankCell c (spaceAdvance chars) // NBSP: width of space
                    else
                        fail $"U+{int c:X4} ({c}) renders empty and is not a known space")

        // Shelf packing, tallest first, 2px gutters against any filtering bleed.
        // Incremental runs continue below the kept rows.
        let gutter = 2

        let startY =
            match kept with
            | [] -> gutter
            | _ -> (kept |> List.map (fun (_, _, y, _, h) -> y + h) |> List.max) + gutter

        let pack width =
            let mutable px, py, rowH = gutter, startY, 0
            let placed = ResizeArray()

            for cell in cells |> List.sortByDescending (fun c -> c.Pixels.GetLength 1) do
                let cw, ch = cell.Pixels.GetLength 0, cell.Pixels.GetLength 1

                if px + cw + gutter > width then
                    px <- gutter
                    py <- py + rowH + gutter
                    rowH <- 0

                placed.Add((cell, px, py))
                px <- px + cw + gutter
                rowH <- max rowH ch

            placed |> Seq.toList, py + rowH + gutter

        let mutable width = 1024

        // Incremental runs keep the previous page width; fresh pages may widen.
        let oldBmp = if kept.IsEmpty then None else Some(SKBitmap.Decode extPath)

        match oldBmp with
        | Some bmp -> width <- bmp.Width
        | None -> ()

        let p1, h1 = pack width
        let mutable placed = p1
        let mutable height = h1

        match oldBmp with
        | Some bmp ->
            height <- max height bmp.Height

            if height > 2048 then
                fail $"atlas overflow at 2048px for {spec.Fnt}"
        | None ->
            if height > 1024 then
                width <- 2048
                let p2, h2 = pack width
                placed <- p2
                height <- h2

            if height > 2048 then
                fail $"atlas overflow at 2048px for {spec.Fnt}"

        printfn $"  page {width}x{height}"

        use surface = SKSurface.Create(new SKImageInfo(width, height))
        let canvas = surface.Canvas
        canvas.Clear(SKColors.Transparent)

        match oldBmp with
        | Some bmp -> canvas.DrawBitmap(bmp, 0f, 0f)
        | None -> ()

        use white = new SKPaint(Color = SKColors.White)
        use black = new SKPaint(Color = SKColors.Black)

        for cell, px, py in placed do
            let cw, ch = cell.Pixels.GetLength 0, cell.Pixels.GetLength 1

            for y in 0 .. ch - 1 do
                for x in 0 .. cw - 1 do
                    canvas.DrawPoint(float32 (px + x), float32 (py + y), (if cell.Pixels.[x, y] then white else black))

        // Halo pass: black where a 3x3 neighborhood holds a white pixel.
        // (Drawn per-pixel above as core only; expand here from the packed page.)
        use img = surface.Snapshot()
        use bmp = SKBitmap.FromImage img

        let get x y =
            if x < 0 || y < 0 || x >= width || y >= height then
                false
            else
                bmp.GetPixel(x, y).Alpha <> 0uy && bmp.GetPixel(x, y).Red = 255uy

        use halo = new SKBitmap(width, height)

        for y in 0 .. height - 1 do
            for x in 0 .. width - 1 do
                if get x y then
                    halo.SetPixel(x, y, SKColors.White)
                else
                    let mutable near = false

                    for dy in -1 .. 1 do
                        for dx in -1 .. 1 do
                            if get (x + dx) (y + dy) then near <- true

                    if near then halo.SetPixel(x, y, SKColors.Black)

        use out = File.OpenWrite(Path.Combine(fontsDir, spec.ExtPage))
        halo.Encode(out, SKEncodedImageFormat.Png, 100) |> ignore

        // Append to the descriptor with plain text surgery, so the original
        // lines stay byte-identical and reviewable. The page element goes in
        // once; every run only appends its own new char lines.
        let mutable fnt = File.ReadAllText fntPath
        fnt <- Regex("pages=\"\\d+\"").Replace(fnt, "pages=\"2\"")

        if kept.IsEmpty then
            let pageLine = $"    <page id=\"1\" file=\"{spec.ExtPage}\" />\n"
            fnt <- fnt.Replace("  </pages>", pageLine + "  </pages>")

        let charLine (cell: Cell) (px: int) (py: int) =
            let cp = Char.ConvertToUtf32(string cell.Char, 0)
            $"    <char id=\"{cp}\" x=\"{px}\" y=\"{py}\" width=\"{cell.Pixels.GetLength 0}\" height=\"{cell.Pixels.GetLength 1}\" xoffset=\"{cell.XOffset}\" yoffset=\"{cell.YOffset}\" xadvance=\"{cell.XAdvance}\" page=\"1\" chnl=\"0\" />\n"

        let charLines = placed |> List.map (fun (cell, px, py) -> charLine cell px py) |> String.concat ""

        let total = (doc.Descendants(xn "char") |> Seq.length) + placed.Length
        fnt <- Regex("""<chars count="\d+">""").Replace(fnt, $"<chars count=\"{total}\">")
        fnt <- fnt.Replace("  </chars>", charLines + "  </chars>")

        File.WriteAllText(fntPath, fnt)

        // Verify the rewritten descriptor instead of trusting the write.
        let reDoc = XDocument.Load fntPath
        let reIds = reDoc.Descendants(xn "char") |> Seq.map (fun e -> Int32.Parse(e.Attribute(xn "id").Value)) |> Set.ofSeq

        for c in needed do
            if not (reIds.Contains(Char.ConvertToUtf32(string c, 0))) then
                fail $"U+{int c:X4} lost in rewritten {spec.Fnt}"

        for e in reDoc.Descendants(xn "char") do
            if e.Attribute(xn "page").Value = "1" then
                let x = Int32.Parse(e.Attribute(xn "x").Value)
                let y = Int32.Parse(e.Attribute(xn "y").Value)
                let w = Int32.Parse(e.Attribute(xn "width").Value)
                let h = Int32.Parse(e.Attribute(xn "height").Value)

                if x + w > width || y + h > height then
                    let cid = e.Attribute(xn "id").Value
                    fail $"char {cid} rect outside the new page"

    // Preview: sample lines through the real metrics, red baselines included.
    // Runs every time, so a no-op rebuild still proves the committed atlas.
    let prevDoc = XDocument.Load fntPath
    let entries = prevDoc.Descendants(xn "char") |> Seq.map (fun e -> Int32.Parse(e.Attribute(xn "id").Value), e) |> Map.ofSeq
    let page0file = (prevDoc.Descendants(xn "page") |> Seq.head).Attribute(xn "file").Value
    Directory.CreateDirectory previewDir |> ignore

    let drawLine (canvas: SKCanvas) (lineTop: int) (s: string) =
        let mutable x = 8

        for c in s do
            match entries.TryFind(Char.ConvertToUtf32(string c, 0)) with
            | None -> x <- x + 8
            | Some e ->
                let page = e.Attribute(xn "page").Value
                let imgPath = Path.Combine(fontsDir, (if page = "0" then page0file else spec.ExtPage))
                use cell = SKBitmap.Decode imgPath
                let sx = Int32.Parse(e.Attribute(xn "x").Value)
                let sy = Int32.Parse(e.Attribute(xn "y").Value)
                let sw = Int32.Parse(e.Attribute(xn "width").Value)
                let sh = Int32.Parse(e.Attribute(xn "height").Value)
                let subset = SKRectI.Create(sx, sy, sw, sh)
                let dx = float32 (x + Int32.Parse(e.Attribute(xn "xoffset").Value))
                let dy = float32 (lineTop + Int32.Parse(e.Attribute(xn "yoffset").Value))
                canvas.DrawBitmap(cell, subset, SKRect.Create(dx, dy, float32 sw, float32 sh))
                x <- x + Int32.Parse(e.Attribute(xn "xadvance").Value)

        x

    let lines =
        [ "ABC abc 123 !?#"
          "Шипы — не помеха Привіт!"
          "Німб їжак ґедзь є"
          "Zażółć gęślą jaźń ŁÓDŹ"
          "ўсё Ў"
          "不仅仅是根棍子，伟大的哥布林！「引」…" ]

    let rowH = spec.LineHeight + 10
    use prev = SKSurface.Create(new SKImageInfo(900, rowH * lines.Length + 16))
    let pc = prev.Canvas
    pc.Clear(new SKColor(30uy, 30uy, 40uy))
    use red = new SKPaint(Color = SKColors.Red)

    lines
    |> List.iteri (fun i s ->
        let top = 8 + i * rowH
        drawLine pc top s |> ignore
        pc.DrawLine(0f, float32 (top + spec.Base), 900f, float32 (top + spec.Base), red))

    let previewName = $"font_preview_{Path.GetFileNameWithoutExtension(spec.Fnt)}.png"
    use pout = File.OpenWrite(Path.Combine(previewDir, previewName))
    use data = prev.Snapshot().Encode(SKEncodedImageFormat.Png, 100)
    data.SaveTo pout

    printfn $"  preview: {previewName}"

for spec in fonts do
    build spec

printfn "done"

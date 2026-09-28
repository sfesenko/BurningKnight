# Burning Knight 

Available on [Steam](https://store.steampowered.com/app/851150/Burning_Knight/) and [itch.io](https://egordorichev.itch.io/bk).

Before you look at the code: hear me out. Yes, some of it is not the best. Yes, some of it can be redone and improved.
But I have to admit, I came so close to dropping this whole project so many times, I still have no idea, how I made it through May of 2020.
Anyway. Here it is. The source code of C# branch of Burning Knight. **For java branch see [this repo](https://github.com/egordorichev/BurningKnightJava).**

###### (Yes, this is only half of the work, the game was initially written in Java and then rewritten in C#, this code base is relatively nice compared to the Java one).

This project really quickly moved from the "nice and relaxing" to "constant bugfixing and stress" type of project.
You can clearly see me going mad in some places (do not look at the pull requests I beg you).
But its all in the past now. Sadly, the game has done really poorly on the Steam, even tho I've tried my best with marketing and everything else.
I'm still really really proud of the game, it's THE project I can point people to and say: I've made this. That's one of the reasons why I wanted to open the source of this monster.

There are not a lot of open source games, that have been released on Steam. So there is not that much where you can go to learn from real released games. Of course, you can decompile games, but it will never compare to the actual source.
I believe in the power open source. I'm opening this project up with hope, that it might turn out to be useful to someone. So yeah, forgive me my code decisions in some places.

Anyway, onto the juicy stuff.

The game has a lot of developer tools in it, but to gain access to them, you must build the `Desktop` project in Debug configuration.
Debug is a development build: the dev tools are enabled, assets are loaded from the source files in `Content`, animations reload while the game runs, and the intro cutscene is skipped — you start at the beginning of the first level. Release is the build that behaves like the Steam version, intro and all.

To access the dev tools, you shall press F1 while being in InGameState (while you are normally playing and not watching a loading screen).
A panel with a bunch of checkboxes should appear, that show different dev tools. Have fun!

##### Why did you merge pull requests into release branch all the time?

You see, this is how I've set up Github Actions CI. The tool went online in the middle of the first summer of development of the C# branch, and it was such a huge help.
Before that, I had to compile all the builds for beta testing by hand, but after 3 days of internal screaming I was able to get the CI working, and from that point I was able just to merge
my dev branch into release, and 10 minutes later press a few buttons on Itch/Steam to release the new builds.

##### Building

You just need the [.NET 10 SDK](https://dotnet.microsoft.com/download) — everything else is restored from NuGet. Open `Lens.sln` in your C# IDE of choice and run the `Desktop` project, or do it from the terminal.

Debug (dev tools enabled, assets loaded from the source tree):

```bash
dotnet run --project Desktop/Desktop.csproj
```

Release (same as on Steam):

```bash
dotnet build Desktop/Desktop.csproj -c Release
Desktop/bin/Release/net10.0/Desktop
```

The game finds its content on its own: it looks for a `Content` directory next to the executable, then for the source tree, and links the content next to the executable when it needs to. Set the `BK_CONTENT` environment variable to point it somewhere else.

##### Content

`Content/` is a code-less project that owns the content targets; `Desktop/` references it, so building the game builds the content.

- **One build step**: `Content/Animations/*.ase` → a PNG per animation plus one `animations.json` in `Content/bin/Animations/` (git-ignored). It runs before the build, skips when nothing changed, and fails on art it cannot blit — or run it alone with `dotnet fsi tools/preprocess-ase.fsx`.
- **Everything else loads as-is**: textures (PNG), sfx (WAV), music, fonts, locales, dialogs, prefabs. Shaders are the one compiled asset — `.xnb` committed, rebuilt with `tools/compile-shaders.sh` (needs MGCB, and Wine on Linux).
- **Where it reads from**: Debug links the source tree beside the executable for hot reload; Release stages a copy into its own output and runs anywhere.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lens.util;
using Lens.util.file;
using Lens.util.tween;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace Lens.assets;

public class Audio
{
    public static readonly Audio Instance = new Audio();
    
    public float MasterVolume = 1;
    public float SfxVolume = 1;
    public float SfxVolumeBuffer = 1f;
    public volatile float  SfxVolumeBufferResetTimer = 0;

    private const float CrossFadeTime = 0.25f;

    public readonly float Db3 = 0.1f;

    private Song? currentPlaying = null;
    private string? currentPlayingMusic = null;
    private Dictionary<string, Song> musicInstances = new();
    private Dictionary<string, SoundEffect> sounds = new();

    public bool Repeat
    {
        get => MediaPlayer.IsRepeating;
        set => MediaPlayer.IsRepeating = value;
    }

    private DynamicSoundEffectInstance SoundEffectInstance;

    public float Speed = 1;

    private Audio()
    {
    }


    private void LoadSfx(FileHandle file, string path, bool root = false)
    {
        if (file.Exists())
        {
            path = $"{path}{file.Name}{(root ? "" : "/")}";

            foreach (var sfx in file.ListFileHandles())
            {
                if (sfx.Extension == ".wav")
                {
                    LoadSound(sfx, path);
                }
            }

            foreach (var dir in file.ListDirectoryHandles())
            {
                LoadSfx(dir, path);
            }
        }
        else
        {
            Log.Error($"File {file.Name} is missing");
        }
    }

    internal void Load()
    {
        Destroy();
        LoadSfx(FileHandle.FromRoot("Sfx/"), "", true);

        Log.Debug($"Loaded {sounds.Count} sounds");
    }

    private void LoadSound(FileHandle file, string path)
    {
        var s = file.NameWithoutExtension;
        var key = $"{path}{s}".Replace('/', '_');

        using (var stream = file.OpenRead())
        {
            sounds[key] = SoundEffect.FromStream(stream);
        }
    }

    // MonoGame hands OpenAL sources out of one shared pool and never clears AL_LOOPING when a source
    // comes back, so a source a looping SoundEffectInstance has used can still be flagged as looping
    // when a Song reserves it. OpenAL then loops the Song's first streaming buffers forever instead
    // of playing through, and nothing in the Song or MediaPlayer API can clear the flag afterwards.
    // A SoundEffectInstance applies IsLooped to the source it plays on, so playing and stopping a
    // silent non-looping one first clears the flag on the source the Song is about to reserve: the
    // pool hands back the most recently returned source, and that is now this one.
    private void ClearLoopFlagOnNextSource()
    {
        if (!Assets.LoadSfx || sounds.Count == 0)
        {
            return;
        }

        foreach (var sound in sounds.Values)
        {
            try
            {
                var instance = sound.CreateInstance();

                if (instance == null)
                {
                    continue;
                }

                instance.Volume = 0;
                instance.IsLooped = false;
                instance.Play();
                instance.Stop();
                instance.Dispose();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return;
        }
    }

    // MonoGame's Song has no stream constructor, so a track that lives in the archive is copied
    // out first — into the writable data directory, never beside the executable. A loose file is
    // used where it is, so the source tree and mods keep working, hot reload included.
    private static string MusicFile(string music)
    {
        var loose = FileHandle.FromRoot($"Music/{music}.ogg");

        if (File.Exists(loose.FullPath))
        {
            return loose.FullPath;
        }

        using var source = Assets.Source.Open($"Music/{music}.ogg");

        if (source == null)
        {
            Log.Error($"Music {music} was not found!");
            return null;
        }

        var directory = Path.Combine(Paths.DataDir, "music");
        var cached = Path.Combine(directory, $"{music}.ogg");

        Directory.CreateDirectory(directory);

        using (var target = File.Create(cached))
        {
            source.CopyTo(target);
        }

        return cached;
    }

    private Song GetOrLoadMusic(string music)
    {
        if (musicInstances.TryGetValue(music, out var song))
        {
            return song;
        }

        ClearLoopFlagOnNextSource();

        var file = MusicFile(music);

        if (file == null)
        {
            return null;
        }

        song = Song.FromUri(music, new Uri(file));
        musicInstances[music] = song;

        return song;
    }

    internal void Destroy()
    {
        foreach (var sound in sounds.Values)
        {
            sound.Dispose();
            GC.SuppressFinalize(sound); // Lol what?
        }

        if (SoundEffectInstance != null)
        {
            Log.Info("Disposing audio mixer");

            SoundEffectInstance.Stop();
            SoundEffectInstance.Dispose();
            GC.SuppressFinalize(SoundEffectInstance);

            SoundEffectInstance = null;
        }
    }

    public void PlaySfx(string id, float volume = 1, float pitch = 0, float pan = 0)
    {
        if (!Engine.Instance.Focused || !Assets.LoadSfx)
        {
            return;
        }

        PlaySfx(GetSfx(id), volume * (id.StartsWith("level_explosion") ? MasterVolume : SfxVolumeBuffer), pitch,
            pan);
    }

    public SoundEffect GetSfx(string id)
    {
        if (sounds.TryGetValue(id, out var effect))
        {
            return effect;
        }

        Log.Error($"Sound effect {id} was not found!");
        return null;
    }

    private void PlaySfx(SoundEffect sfx, float volume = 1, float pitch = 0, float pan = 0)
    {
        if (!Assets.LoadSfx)
        {
            return;
        }

        sfx?.Play(MathUtils.Clamp(0, 1, volume * SfxVolume * MasterVolume), pitch, pan);
    }

    public void PlayMusic(string music, bool fromStart = true)
    {
        if (!Assets.LoadMusic)
        {
            return;
        }

        if (currentPlayingMusic == music && currentPlaying != null)
        {
            return;
        }

        Repeat = true;

        if (!fromStart)
        {
            FadeOut(() => { LoadAndPlayMusic(music, fromStart); });
        }
        else
        {
            LoadAndPlayMusic(music, fromStart);
        }
    }

    private bool loading;

    private void LoadAndPlayMusic(string music, bool fromStart = false)
    {
        try
        {
            loading = true;
            
            var id = Environment.CurrentManagedThreadId;
            Log.Info($"Audio.Play: {id}");

            currentPlaying = GetOrLoadMusic(music);

            if (currentPlaying == null)
            {
                loading = false;
                return;
            }

            MediaPlayer.Pause();
            MediaPlayer.Play(currentPlaying);

            Log.Info($"Playing music {music} repeat = {Repeat}");
            currentPlayingMusic = music;

            Tween.To(musicVolume, MediaPlayer.Volume, x => MediaPlayer.Volume = x,
                fromStart ? 0.05f : CrossFadeTime);

            loading = false;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load {music}");
            Log.Error(e);
            loading = false;
        }
    }

    private void ThreadLoad(string music, bool fromStart = false)
    {
        try
        {
            loading = true;
            
            var id = Environment.CurrentManagedThreadId;
            Log.Info($"Audio.Play: {id}");

            if (!musicInstances.TryGetValue(music, out currentPlaying))
            {
                Log.Debug($"ThreadLoad: loading {music}");
                currentPlaying = Song.FromUri(music, new Uri(MusicFile(music)));
                musicInstances[music] = currentPlaying;
            }

            MediaPlayer.Pause();
            MediaPlayer.Play(currentPlaying);

            Log.Info($"Playing music {music} repeat = {Repeat}");
            currentPlayingMusic = music;

            Tween.To(musicVolume, MediaPlayer.Volume, x => MediaPlayer.Volume = x,
                fromStart ? 0.05f : CrossFadeTime);

            loading = false;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load {music}");
            Log.Error(e);
            loading = false;
        }
    }

    public void FadeOut(Action? callback = null)
    {
        if (currentPlaying != null)
        {
            Tween.To(0, MediaPlayer.Volume, x => MediaPlayer.Volume = x, CrossFadeTime).OnEnd = () =>
            {
                currentPlaying = null;
                currentPlayingMusic = null;
                var mediaState = MediaPlayer.State;
                Log.Debug($@"MediaState: {mediaState}");
                if (mediaState == MediaState.Playing)
                {
                    // MediaPlayer.Stop();
                }

                callback?.Invoke();
            };
        }
        else
        {
            callback?.Invoke();
        }
    }

    public void Stop()
    {
        var id = Environment.CurrentManagedThreadId;
        Log.Info($"Audio.Stop: {id}");
        // MediaPlayer.Stop();

        currentPlaying = null;
        currentPlayingMusic = null;
    }

    private float musicVolume = 1;

    public void UpdateMusicVolume(float value)
    {
        MediaPlayer.Volume = value;
        musicVolume = value;
    }

    public void Update(float dt)
    {
        if (SfxVolumeBufferResetTimer > 0)
        {
            SfxVolumeBufferResetTimer -= dt;

            if (SfxVolumeBufferResetTimer <= 0)
            {
                Tween.To(1, SfxVolumeBuffer, x => SfxVolumeBuffer = x, 0.3f);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lens.util;
using Lens.util.file;
using Lens.util.tween;
using Microsoft.Xna.Framework.Audio;

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

    private MusicPlayer currentPlaying = null;
    private string? currentPlayingMusic = null;
    private Dictionary<string, MusicPlayer> musicInstances = new();
    private Dictionary<string, SoundEffect> sounds = new();

    // The player loops on its own; the flag stays because PlayMusic sets it and the log names it.
    public bool Repeat { get; set; } = true;

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
    // when the music player reserves it. OpenAL then loops the track's first buffers forever
    // instead of playing through, and nothing in the audio API can clear the flag afterwards.
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

    // MonoGame's Song opens from a path only, so the archive's music is played by a MusicPlayer
    // instead — NVorbis reads the entry straight from the content source, and nothing is written
    // out to disk.
    private MusicPlayer GetOrLoadMusic(string music)
    {
        if (musicInstances.TryGetValue(music, out var player))
        {
            return player;
        }

        ClearLoopFlagOnNextSource();

        player = new MusicPlayer(music);

        if (!player.Ready)
        {
            return null;
        }

        musicInstances[music] = player;

        return player;
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

            currentPlaying.Play();
            currentPlaying.Volume = musicVolume;

            Log.Info($"Playing music {music} repeat = {Repeat}");
            currentPlayingMusic = music;

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
            var player = currentPlaying;

            Tween.To(0, player.Volume, x => player.Volume = x, CrossFadeTime).OnEnd = () =>
            {
                currentPlaying = null;
                currentPlayingMusic = null;
                Log.Debug($@"Music state: {player.State}");
                player.Stop();

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

        currentPlaying?.Stop();
        currentPlaying = null;
        currentPlayingMusic = null;
    }

    private float musicVolume = 1;

    public void UpdateMusicVolume(float value)
    {
        musicVolume = value;

        if (currentPlaying != null)
        {
            currentPlaying.Volume = value;
        }
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
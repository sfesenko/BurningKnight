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
    public float MasterVolume = 1;
    public float SfxVolume = 1;
    public float SfxVolumeBuffer = 1f;
    public volatile float  SfxVolumeBufferResetTimer = 0;

    private const float CrossFadeTime = 0.25f;

    public readonly float Db3 = 0.1f;

    private MusicPlayer? currentPlaying;
    private string? currentPlayingMusic = null;
    private Dictionary<string, MusicPlayer> musicInstances = new();
    private readonly HashSet<string> failedMusic = new();
    private Dictionary<string, SoundEffect> sounds = new();

    // The player loops on its own; the flag stays because PlayMusic sets it and the log names it.
    public bool Repeat { get; set; } = true;

    private DynamicSoundEffectInstance? SoundEffectInstance;

    public float Speed = 1;

    public Audio()
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

        // Empty bank (broken device or content): stop pretending audio works, or every
        // trigger logs a miss forever and the "Audio Failed" notice never fires.
        if (sounds.Count == 0 && Assets.LoadSfx) {
            Assets.LoadSfx = false;
            Assets.FailedToLoadAudio = true;
        }
    }

    private void LoadSound(FileHandle file, string path)
    {
        var s = file.NameWithoutExtension;
        var key = $"{path}{s}".Replace('/', '_');

        // One corrupt file must not take the whole bank with it. Inline on the worker,
        // so this catch is effective.
        try
        {
            using (var stream = file.OpenRead())
            {
                if (stream == null) {
                    Log.Error($"Sound file {file.Name} was not found, skipping");

                    return;
                }

                sounds[key] = SoundEffect.FromStream(stream);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load sound {key}: {e}");
        }
    }

    // The source pool never clears AL_LOOPING on reuse: a looping instance's source can come
    // back still flagged, making the music track loop its first buffers forever. Play+stop a
    // silent non-looping instance first — the pool hands back the most recently returned
    // source, so the flag is cleared on the one the music is about to reserve.
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

    // Song opens from a path only; NVorbis plays the archive entry instead — nothing written out.
    private MusicPlayer? GetOrLoadMusic(string music)
    {
        if (musicInstances.TryGetValue(music, out var player))
        {
            return player;
        }

        if (failedMusic.Contains(music))
        {
            return null;
        }

        ClearLoopFlagOnNextSource();

        player = new MusicPlayer(music);

        if (!player.Ready)
        {
            // Missing file or dead device: don't re-read the whole ogg every PlayMusic.
            failedMusic.Add(music);

            return null;
        }

        musicInstances[music] = player;

        return player;
    }

    internal void Destroy()
    {
        foreach (var sound in sounds.Values)
        {
            try {
                sound.Dispose();
                GC.SuppressFinalize(sound); // Lol what?
            } catch (Exception e) {
                // A dead effect must not break teardown or leak the rest.
                Log.Error(e);
            }
        }

        sounds.Clear();

        foreach (var player in musicInstances.Values)
        {
            try {
                player.Stop();
                player.Dispose();
            } catch (Exception e) {
                // A dead voice must not break teardown or leak the rest.
                Log.Error(e);
            }
        }

        musicInstances.Clear();
        failedMusic.Clear();
        currentPlaying = null;
        currentPlayingMusic = null;

        if (SoundEffectInstance != null)
        {
            Log.Info("Disposing audio mixer");

            try {
                SoundEffectInstance.Stop();
                SoundEffectInstance.Dispose();
                GC.SuppressFinalize(SoundEffectInstance);
            } catch (Exception e) {
                Log.Error(e);
            }

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

    public SoundEffect? GetSfx(string id)
    {
        if (sounds.TryGetValue(id, out var effect))
        {
            return effect;
        }

        Log.Error($"Sound effect {id} was not found!");
        return null;
    }

    private void PlaySfx(SoundEffect? sfx, float volume = 1, float pitch = 0, float pan = 0)
    {
        if (!Assets.LoadSfx)
        {
            return;
        }

        try {
            sfx?.Play(MathUtils.Clamp(0, 1, volume * SfxVolume * MasterVolume), pitch, pan);
        } catch (InstancePlayLimitException) {
            // Pool exhausted; a dropped sound must not kill the run.
        } catch (Exception e) {
            // Dead/disposed effect: same, never fatal.
            Log.Error(e);
        }
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


    private void LoadAndPlayMusic(string music, bool fromStart = false)
    {
        try
        {
            
            var id = Environment.CurrentManagedThreadId;
            Log.Info($"Audio.Play: {id}");

            var next = GetOrLoadMusic(music);

            if (next == null)
            {
                return;
            }

            // One track at a time; stopping the others also covers a fade this track interrupted.
            foreach (var player in musicInstances.Values)
            {
                // State/Stop can throw on a dead device; neither may abort the new track below.
                try {
                    if (player != next && player.State == SoundState.Playing)
                    {
                        player.Stop();
                    }
                } catch (Exception e) {
                    Log.Error(e);
                }
            }

            currentPlaying = next;
            next.Play();
            next.Volume = musicVolume;

            Log.Info($"Playing music {music} repeat = {Repeat}");
            currentPlayingMusic = music;

        }
        catch (Exception e)
        {
            Log.Error($"Failed to load {music}");
            Log.Error(e);
        }
    }

    public void FadeOut(Action? callback = null)
    {
        if (currentPlaying != null)
        {
            var player = currentPlaying;

            Tween.To(0, player.Volume, x => player.Volume = x, CrossFadeTime).OnEnd = () =>
            {
                player.Stop();

                // Only clear if this fade still owns the state; a track started during it isn't forgotten.
                if (currentPlaying == player)
                {
                    currentPlaying = null;
                    currentPlayingMusic = null;
                }

                Log.Debug($@"Music state: {player.State}");
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

        foreach (var player in musicInstances.Values)
        {
            try {
                player.Stop();
            } catch (Exception e) {
                // Dead voice: must not skip the remaining voices or the reset below.
                Log.Error(e);
            }
        }

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
﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;

namespace SiliconStudio.Xenko.Audio
{
    /// <summary>
    /// Represents the audio engine. 
    /// In current version, the audio engine necessarily creates its context on the default audio hardware of the device.
    /// The audio engine is required when creating or loading sounds.
    /// </summary>
    /// <remarks/>The AudioEngine is Disposable. Call the <see cref="ComponentBase.Dispose"/> function when you do not need to play sounds anymore to free memory allocated to the audio system. 
    /// A call to Dispose automatically stops and disposes all the <see cref="Sound"/>, <see cref="SoundInstance"/>
    public class AudioEngine : ComponentBase
    {
        public AudioListener DefaultListener;

        private readonly AudioDevice audioDevice;

        static AudioEngine()
        {
            if (!Native.AudioLayer.Init())
            {
                throw new Exception("Failed to initialize the OpenAL native layer.");
            }
        }

        /// <summary>
        /// The logger of the audio engine.
        /// </summary>
        public static readonly Logger Logger = GlobalLogger.GetLogger("AudioEngine");

        /// <summary>
        /// Create an Audio Engine on the default audio device
        /// </summary>
        /// <param name="sampleRate">The desired sample rate of the audio graph. 0 let the engine choose the best value depending on the hardware.</param>
        /// <exception cref="AudioInitializationException">Initialization of the audio engine failed. May be due to memory problems or missing audio hardware.</exception>
        public AudioEngine(uint sampleRate = 0)
            : this(new AudioDevice(), sampleRate)
        {
        }

        /// <summary>
        /// Create the Audio Engine on the specified device.
        /// </summary>
        /// <param name="device">Device on which to create the audio engine.</param>
        /// <param name="sampleRate">The desired sample rate of the audio graph. 0 let the engine choose the best value depending on the hardware.</param>
        /// <exception cref="AudioInitializationException">Initialization of the audio engine failed. May be due to memory problems or missing audio hardware.</exception>
        public AudioEngine(AudioDevice device, uint sampleRate = 0)
        {
            State = AudioEngineState.Running;

            AudioSampleRate = sampleRate;

            audioDevice = device;
        }

        internal Native.AudioLayer.Device AudioDevice;

        /// <summary>
        /// Initialize audio engine
        /// </summary>
        public virtual void InitializeAudioEngine()
        {
            AudioDevice = Native.AudioLayer.Create(audioDevice.Name == "default" ? null : audioDevice.Name);
            if (AudioDevice.Ptr == IntPtr.Zero)
            {
                State = AudioEngineState.Invalidated;
            }

            DefaultListener = new AudioListener(this);
        }

        /// <summary>
        /// Platform specifc implementation of <see cref="Destroy"/>.
        /// </summary>
        internal void DestroyAudioEngine()
        {
            if (AudioDevice.Ptr != IntPtr.Zero)
            {
                Native.AudioLayer.ListenerDestroy(DefaultListener.Listener);
                Native.AudioLayer.Destroy(AudioDevice);
            }
        }
        
        /// <summary>
        /// The list of the sounds that have been paused by the call to <see cref="PauseAudio"/> and should be resumed by <see cref="ResumeAudio"/>.
        /// </summary>
        private readonly List<SoundInstance> pausedSounds = new List<SoundInstance>();

        /// <summary>
        /// The underlying sample rate of the audio system.
        /// </summary>
        internal uint AudioSampleRate { get; private set; }

        /// <summary>
        /// Method that updates all the sounds play status. 
        /// </summary>
        /// <remarks>Should be called in same thread as user main thread.</remarks>
        /// <exception cref="InvalidOperationException">One or several of the sounds asked for play had invalid data (corrupted or unsupported formats).</exception>
        public void Update()
        {
        }

        /// <summary>
        /// The current state of the <see cref="AudioEngine"/>.
        /// </summary>
        public AudioEngineState State { get; protected set; }

        /// <summary>
        /// Pause the audio engine. That is, pause all the currently playing <see cref="SoundInstance"/>, and block any future play until <see cref="ResumeAudio"/> is called.
        /// </summary>
        public void PauseAudio()
        {
            if(State != AudioEngineState.Running)
                return;

            State = AudioEngineState.Paused;

            pausedSounds.Clear();
            lock (notDisposedSounds)
            {
                foreach (var sound in notDisposedSounds)
                {
                    foreach (var instance in sound.Instances)
                    {
                        if (instance.PlayState == SoundPlayState.Playing)
                        {
                            instance.Pause();
                            pausedSounds.Add(instance);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resume all audio engine. That is, resume the sounds paused by <see cref="PauseAudio"/>, and re-authorize future calls to play.
        /// </summary>
        public void ResumeAudio()
        {
            if(State != AudioEngineState.Paused)
                return;

            State = AudioEngineState.Running;

            foreach (var playableSound in pausedSounds)
            {
                if (!playableSound.IsDisposed && playableSound.PlayState == SoundPlayState.Paused) // sounds can have been stopped by user while the audio engine was paused.
                    playableSound.Play();
            }
        }

        private readonly List<Sound> notDisposedSounds = new List<Sound>(); 

        internal void RegisterSound(Sound newSound)
        {
            lock (notDisposedSounds)
            {
                notDisposedSounds.Add(newSound);
            }
        }

        internal void UnregisterSound(Sound disposedSound)
        {
            lock (notDisposedSounds)
            {
                if (!notDisposedSounds.Remove(disposedSound))
                    throw new AudioSystemInternalException("Try to remove a disposed sound not in the list of registered sounds.");
            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            if (IsDisposed)
                return;

            Sound[] notDisposedSoundsArray;
            lock (notDisposedSounds)
            {
                notDisposedSoundsArray = notDisposedSounds.ToArray();
            }

            // Dispose all the sound not disposed yet.
            foreach (var soundBase in notDisposedSoundsArray)
                soundBase.Dispose();

            DestroyAudioEngine();

            State = AudioEngineState.Disposed;
        }
    }
}

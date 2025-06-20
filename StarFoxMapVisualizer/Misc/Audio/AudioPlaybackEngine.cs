using System;
using System.IO;
using System.Windows;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace StarFoxMapVisualizer.Misc.Audio
{
    internal class AudioPlaybackEngine : IDisposable
    {
        private readonly IWavePlayer outputDevice;
        private readonly MixingSampleProvider mixer;

        public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
        {
            outputDevice = new WaveOutEvent();
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));
            mixer.ReadFully = true;
            try {
				outputDevice.Init(mixer); 
				outputDevice.Play();
            } catch (MmException) {
	            MessageBox.Show("Failed to initialize audio. You may need to plug your headphone jack and or start the Windows Audio service (especially on Windows Server).");
            }
        }

        public void PlaySound(string fileName)
        {
            var input = new AudioFileReader(fileName);
            AddMixerInput(new AutoDisposeFileReader(input));
        }

        public void PlaySound(Stream WavStream)
        {
            var input = new WaveFileReader(WavStream);
            AddMixerInput(input.ToSampleProvider());
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }

        private void AddMixerInput(ISampleProvider input)
        {
            mixer.AddMixerInput(ConvertToRightChannelCount(input));
        }

        public void Dispose()
        {
            outputDevice.Dispose();
        }
    }
}

using SoundFingerprinting.Audio;
using SoundFingerprinting.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace AudioDuplicateScan
{
    internal class AVHashSegmenter
    {
        AudioSamples samples;
        public AVHashSegmenter(AudioSamples samples)
        {
            this.samples = samples;
        }

        public AudioSamples getFloatSegment(int start, int len)
        {
            int rangeStart = (start * samples.SampleRate);
            int rangeLen = (len * samples.SampleRate);
            float[] subset = new float[rangeLen];
            Array.Copy(samples.Samples, rangeStart, subset, 0, rangeLen);
            return new AudioSamples(subset, "s", samples.SampleRate);
        }
    }
}

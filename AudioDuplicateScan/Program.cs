// See https://aka.ms/new-console-template for more information
using SoundFingerprinting.Audio;
using SoundFingerprinting.InMemory;
using SoundFingerprinting;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using AudioDuplicateScan;
using System;

string SecondsToTs(int secs)
{
    int minutes = secs / 60;
    int seconds = secs % 60;
    return minutes + ":" + seconds;
}

string lastArg = Environment.GetCommandLineArgs().Last();
if (!lastArg.EndsWith(".wav"))
{
    Console.Error.WriteLine("must be a wave file");
    return;
}
if (!File.Exists(lastArg))
{
    Console.Error.WriteLine("path does not exist");
    return;
}

string absolutePath = Path.GetFullPath(lastArg);
IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM
IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library

AudioSamples samples = audioService.ReadMonoSamplesFromFile(lastArg, 5512);

 AVHashes avHashes =await FingerprintCommandBuilder.Instance
         .BuildFingerprintCommand()
         .From(samples)
         .UsingServices(audioService)
         .Hash();

var track1 = new TrackInfo("1", "Input", "input");
modelService.Insert(track1, avHashes);


int secondsToAnalyze = 20; // number of seconds to analyze from query file
List<int> matches = new List<int>();

AVHashSegmenter segmenter = new AVHashSegmenter(samples);

for (int ii = 0; ii < (samples.Duration - secondsToAnalyze); ii = ii + 4)
{
    
    int startAtSecond = ii; // start at the begining

    // query the underlying database for similar audio sub-fingerprints
    var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                         .From(segmenter.getFloatSegment(startAtSecond, secondsToAnalyze))
                                         .UsingServices(modelService, audioService)
                                         .Query();
    int confidentMatches = 0;
    foreach (var match in queryResult.ResultEntries)
    {
        if (match.Audio.QueryRelativeCoverage > 0.9)
        {
            confidentMatches++;
        }
    }
    if (confidentMatches > 1)
    {
        matches.Add(startAtSecond);
    }
}

ASegment? thisSegment = null;
List<ASegment> segments = new List<ASegment>();

foreach (var m in matches)
{
    if (thisSegment == null || m > thisSegment.end)
    {
        thisSegment = new ASegment();
        segments.Add(thisSegment);
        thisSegment.start = m;
        thisSegment.end = m + secondsToAnalyze;
    } else if (m <= thisSegment.end)
    {
        thisSegment.end = m + secondsToAnalyze;
    }
}
foreach (var segment in segments)
{
    Console.Error.WriteLine("BEGIN " + SecondsToTs(segment.start) + " - " + SecondsToTs(segment.end));
}

for (int ii=0; ii < segments.Count; ii++)
{
    var segment = segments[ii];
    if (ii == 0 && segment.start > 0){
        Console.WriteLine("file in.mp3");
        Console.WriteLine("inpoint 0");
        Console.WriteLine("outpoint " + segment.start);
        
    }
    else if (ii > 0)
    {
        Console.WriteLine("file in.mp3");
        var lastSegment = segments[ii - 1];
        Console.WriteLine("inpoint " + lastSegment.end);
        Console.WriteLine("outpoint " + segment.start);
    }
}
if ((samples.Duration - segments.Last().end) > 5)
{
    Console.WriteLine("file in.mp3");
    Console.WriteLine("inpoint " + segments.Last().end);
}

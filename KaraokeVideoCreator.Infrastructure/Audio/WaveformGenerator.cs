using System;
using System.IO;
using KaraokeVideoCreator.Domain.Models;
using KaraokeVideoCreator.Infrastructure.Logging;
using NAudio.Wave;

namespace KaraokeVideoCreator.Infrastructure.Audio
{
    public static class WaveformGenerator
    {
        /// <summary>
        /// Decodes actual audio file using NAudio AudioFileReader and extracts real PCM min/max amplitude peaks.
        /// </summary>
        public static WaveformPoint[] ExtractWaveformPoints(string? filePath, int binCount = 1200)
        {
            if (binCount <= 0) binCount = 1200;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return Array.Empty<WaveformPoint>();
            }

            try
            {
                using var reader = new AudioFileReader(filePath);
                int channels = reader.WaveFormat.Channels;
                long totalSamples = reader.Length / (reader.WaveFormat.BitsPerSample / 8);
                long totalFrames = totalSamples / channels;
                if (totalFrames <= 0) return Array.Empty<WaveformPoint>();

                long framesPerBin = Math.Max(1, totalFrames / binCount);
                int samplesPerBin = (int)(framesPerBin * channels);

                WaveformPoint[] points = new WaveformPoint[binCount];
                float[] readBuffer = new float[samplesPerBin];

                for (int i = 0; i < binCount; i++)
                {
                    int bytesToRead = readBuffer.Length;
                    int readCount = reader.Read(readBuffer, 0, bytesToRead);
                    if (readCount <= 0)
                    {
                        points[i] = new WaveformPoint(0f, 0f);
                        continue;
                    }

                    float min = 0f;
                    float max = 0f;

                    for (int s = 0; s < readCount; s++)
                    {
                        float sample = readBuffer[s];
                        if (sample < min) min = sample;
                        if (sample > max) max = sample;
                    }

                    points[i] = new WaveformPoint(min, max);
                }

                AppLogger.LogInfo($"Successfully extracted {points.Length} real PCM waveform points using NAudio for: {filePath}");
                return points;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to decode audio file for waveform extraction: {filePath}", ex);
                return Array.Empty<WaveformPoint>();
            }
        }
    }
}

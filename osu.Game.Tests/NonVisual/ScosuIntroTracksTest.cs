// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using osu.Framework.IO.Stores;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Screens.Menu;
using osu.Game.Seasonal;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// scosu ships its own intro music (osu.Game/Resources/Tracks/*.osz) in place of osu!'s. The intros find their beatmap by the hash of its .osu files, and
    /// are timed against the track, so both have to stay in sync with what is embedded.
    /// </summary>
    [TestFixture]
    public partial class ScosuIntroTracksTest
    {
        private static readonly string[] forbidden = { "osu!", "ppy", "peppy", "nekodex", "cysmix", "pishifat" };

        private static IResourceStore<byte[]> createStore() => new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(OsuGameBase).Assembly), "Resources");

        private static string[] getHashedOsuFiles(ZipArchive archive) => archive.Entries.Where(e => e.FullName.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)).Select(e => e.FullName).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        // file, first timing point (ms), bpm, minimum length (s)
        [TestCase("circles.osz", 2558.8215, 184.0, 130)]
        [TestCase("triangles.osz", 3011.7401, 160.0, 120)]
        [TestCase("welcome.osz", 0.0, 120.0, 45)]
        [TestCase("christmas2024.osz", 5923.5, 172.0, 100)]
        public void TestTrackIsValid(string file, double firstBeat, double bpm, double minimumLengthSeconds)
        {
            using var stream = createStore().GetStream($"Tracks/{file}");
            Assert.That(stream, Is.Not.Null, $"{file} is not embedded in osu.Game");

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            string[] osuFiles = getHashedOsuFiles(archive);
            Assert.That(osuFiles, Has.Length.EqualTo(1));

            string osuText;

            using (var reader = new StreamReader(archive.GetEntry(osuFiles[0])!.Open()))
                osuText = reader.ReadToEnd();

            Assert.That(osuText.Contains("0,0,\"", StringComparison.Ordinal), Is.False, "the intro beatmap should not have a background");

            using var osuStream = archive.GetEntry(osuFiles[0])!.Open();
            using var lineReader = new LineBufferedReader(osuStream);
            var beatmap = Decoder.GetDecoder<Beatmap>(lineReader).Decode(lineReader);
            var metadata = beatmap.BeatmapInfo.Metadata;

            Assert.That(metadata.Artist, Is.EqualTo("scosu"));
            Assert.That(metadata.Author.Username, Is.EqualTo("scosu"));
            Assert.That(metadata.Title, Does.StartWith("scosu intro"));
            Assert.That(metadata.BackgroundFile, Is.Null.Or.Empty);

            foreach (string text in new[] { metadata.Title, metadata.TitleUnicode, metadata.Artist, metadata.ArtistUnicode, metadata.Author.Username, metadata.Source, metadata.Tags, beatmap.BeatmapInfo.DifficultyName })
            {
                foreach (string bad in forbidden)
                    Assert.That(text ?? string.Empty, Does.Not.Contain(bad).IgnoreCase, $"metadata mentions {bad}");
            }

            // the audio the beatmap points at must be inside the archive.
            Assert.That(metadata.AudioFile, Is.Not.Null.And.Not.Empty);
            var audio = archive.GetEntry(metadata.AudioFile);
            Assert.That(audio, Is.Not.Null, $"{metadata.AudioFile} is missing from {file}");
            Assert.That(audio!.Length, Is.GreaterThan(100_000));

            var timing = beatmap.ControlPointInfo.TimingPoints;
            Assert.That(timing, Is.Not.Empty);
            Assert.That(timing[0].Time, Is.EqualTo(firstBeat).Within(0.1));
            Assert.That(timing[0].BPM, Is.EqualTo(bpm).Within(0.001));

            // ogg vorbis: the identification header and a plausible size for the length (>= ~100 kbps)
            using var audioStream = audio.Open();
            byte[] head = new byte[4];
            Assert.That(audioStream.Read(head, 0, 4), Is.EqualTo(4));
            Assert.That(System.Text.Encoding.ASCII.GetString(head), Is.EqualTo("OggS"));
            Assert.That(audio.Length * 8 / 1000.0 / minimumLengthSeconds, Is.GreaterThan(100));
        }

        [Test]
        public void TestIntrosFindTheirBeatmaps()
        {
            assertHash("circles.osz", new HashExposingCircles().Hash);
            assertHash("triangles.osz", new HashExposingTriangles().Hash);
            assertHash("welcome.osz", new HashExposingWelcome().Hash);
            assertHash("christmas2024.osz", IntroChristmas.CHRISTMAS_BEATMAP_SET_HASH);
        }

        private static void assertHash(string file, string expected)
        {
            using var stream = createStore().GetStream($"Tracks/{file}");
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            // same as RealmArchiveModelImporter.ComputeHash: the hashable files (.osu), ordered by name, concatenated.
            using var hashable = new MemoryStream();

            foreach (string name in getHashedOsuFiles(archive))
            {
                using var entry = archive.GetEntry(name)!.Open();
                entry.CopyTo(hashable);
            }

            string actual = Convert.ToHexString(SHA256.HashData(hashable.ToArray())).ToLowerInvariant();
            Assert.That(actual, Is.EqualTo(expected), $"{file} no longer matches the hash its intro looks for");
        }

        private partial class HashExposingCircles : IntroCircles
        {
            public string Hash => BeatmapHash;
        }

        private partial class HashExposingTriangles : IntroTriangles
        {
            public string Hash => BeatmapHash;
        }

        private partial class HashExposingWelcome : IntroWelcome
        {
            public string Hash => BeatmapHash;
        }
    }
}

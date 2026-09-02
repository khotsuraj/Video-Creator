using System;
using System.IO;
using KaraokeVideoCreator.Infrastructure.Storage;
using Xunit;

namespace KaraokeVideoCreator.Tests
{
    public class RecentProjectsTests
    {
        [Fact]
        public void RecentProjects_AddAndLimit_EnforcesMaxCountAndOrder()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"recent_{Guid.NewGuid()}.json");
            try
            {
                var store = new ApplicationSettingsStore(tempFile);

                for (int i = 1; i <= 15; i++)
                {
                    store.AddRecentProject($@"C:\Projects\Song{i}.kproj", $"Song {i}");
                }

                var recents = store.GetRecentProjects();
                Assert.Equal(10, recents.Count);
                Assert.Equal("Song 15", recents[0].Name);

                store.RemoveRecentProject(@"C:\Projects\Song15.kproj");
                var updated = store.GetRecentProjects();
                Assert.Equal(9, updated.Count);
                Assert.Equal("Song 14", updated[0].Name);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}

namespace KaraokeVideoCreator.Domain.Models
{
    public class ProjectSettings
    {
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public int Fps { get; set; } = 30;

        public ProjectSettings Clone()
        {
            return new ProjectSettings
            {
                Width = Width,
                Height = Height,
                Fps = Fps
            };
        }
    }
}

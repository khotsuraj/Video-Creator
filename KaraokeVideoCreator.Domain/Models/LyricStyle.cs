namespace KaraokeVideoCreator.Domain.Models
{
    public class LyricStyle
    {
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 28.0;
        public string FontWeight { get; set; } = "Normal";
        public string FontStyle { get; set; } = "Normal";
        public string TextColor { get; set; } = "#FFFFFF";
        public string Alignment { get; set; } = "Center";

        public LyricStyle Clone()
        {
            return new LyricStyle
            {
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontWeight = FontWeight,
                FontStyle = FontStyle,
                TextColor = TextColor,
                Alignment = Alignment
            };
        }
    }
}

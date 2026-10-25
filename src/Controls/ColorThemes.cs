using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace DesktopStickyNote.Controls
{
    public class NoteTheme
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Brush HeaderBrush { get; set; }
        public Brush BodyBrush { get; set; }
        public Brush BorderBrush { get; set; }
        public Brush TextBrush { get; set; }
        public Brush ButtonHoverBrush { get; set; }
        public Brush HeaderButtonForeground { get; set; }
        public Brush ScrollThumbBrush { get; set; }
        public Color SwatchColor { get; set; }
        public bool IsDark { get; set; }
    }

    public static class ColorThemes
    {
        private static readonly Dictionary<string, NoteTheme> _themes = new Dictionary<string, NoteTheme>(StringComparer.OrdinalIgnoreCase);

        static ColorThemes()
        {
            RegisterTheme(new NoteTheme
            {
                Name = "Yellow",
                DisplayName = "Classic Yellow",
                HeaderBrush = CreateSolidBrush("#FFF099"),
                BodyBrush = CreateSolidBrush("#FFF8CC"),
                BorderBrush = CreateSolidBrush("#E0D283"),
                TextBrush = CreateSolidBrush("#202020"),
                ButtonHoverBrush = CreateSolidBrush("#EADF82"),
                HeaderButtonForeground = CreateSolidBrush("#555025"),
                ScrollThumbBrush = CreateSolidBrush("#55826A00"),
                SwatchColor = (Color)ColorConverter.ConvertFromString("#FFF099"),
                IsDark = false
            });

            RegisterTheme(new NoteTheme
            {
                Name = "Mint",
                DisplayName = "Soft Mint",
                HeaderBrush = CreateSolidBrush("#C6EFCE"),
                BodyBrush = CreateSolidBrush("#E2F8E7"),
                BorderBrush = CreateSolidBrush("#A4DCB0"),
                TextBrush = CreateSolidBrush("#163A1D"),
                ButtonHoverBrush = CreateSolidBrush("#B3E4BD"),
                HeaderButtonForeground = CreateSolidBrush("#22552C"),
                ScrollThumbBrush = CreateSolidBrush("#55205528"),
                SwatchColor = (Color)ColorConverter.ConvertFromString("#C6EFCE"),
                IsDark = false
            });

            RegisterTheme(new NoteTheme
            {
                Name = "Blue",
                DisplayName = "Sky Blue",
                HeaderBrush = CreateSolidBrush("#BFE0F7"),
                BodyBrush = CreateSolidBrush("#E0F0FC"),
                BorderBrush = CreateSolidBrush("#A2CCE9"),
                TextBrush = CreateSolidBrush("#123854"),
                ButtonHoverBrush = CreateSolidBrush("#A8D3F2"),
                HeaderButtonForeground = CreateSolidBrush("#1D4D73"),
                ScrollThumbBrush = CreateSolidBrush("#551C4B6E"),
                SwatchColor = (Color)ColorConverter.ConvertFromString("#BFE0F7"),
                IsDark = false
            });

            RegisterTheme(new NoteTheme
            {
                Name = "Lavender",
                DisplayName = "Lavender",
                HeaderBrush = CreateSolidBrush("#DFD3F4"),
                BodyBrush = CreateSolidBrush("#F0E9FC"),
                BorderBrush = CreateSolidBrush("#C6B2E6"),
                TextBrush = CreateSolidBrush("#34224F"),
                ButtonHoverBrush = CreateSolidBrush("#CFBFE8"),
                HeaderButtonForeground = CreateSolidBrush("#4D3473"),
                ScrollThumbBrush = CreateSolidBrush("#554A2D6E"),
                SwatchColor = (Color)ColorConverter.ConvertFromString("#DFD3F4"),
                IsDark = false
            });

            RegisterTheme(new NoteTheme
            {
                Name = "Peach",
                DisplayName = "Warm Peach",
                HeaderBrush = CreateSolidBrush("#FAD3C3"),
                BodyBrush = CreateSolidBrush("#FDEBE2"),
                BorderBrush = CreateSolidBrush("#EBB39C"),
                TextBrush = CreateSolidBrush("#4E2314"),
                ButtonHoverBrush = CreateSolidBrush("#EEC0AE"),
                HeaderButtonForeground = CreateSolidBrush("#6B321E"),
                ScrollThumbBrush = CreateSolidBrush("#556B2D17"),
                SwatchColor = (Color)ColorConverter.ConvertFromString("#FAD3C3"),
                IsDark = false
            });

            RegisterTheme(new NoteTheme
            {
                Name = "Dark",
                DisplayName = "Charcoal Dark",
                HeaderBrush = CreateSolidBrush("#2A2A2B"),
                BodyBrush = CreateSolidBrush("#1E1E1E"),
                BorderBrush = CreateSolidBrush("#454545"),
                TextBrush = CreateSolidBrush("#ECECEC"),
                ButtonHoverBrush = CreateSolidBrush("#3D3D3E"),
                HeaderButtonForeground = CreateSolidBrush("#C5C5C5"),
                ScrollThumbBrush = CreateSolidBrush("#55888888"),
                SwatchColor = (Color)ColorConverter.ConvertFromString("#2A2A2B"),
                IsDark = true
            });
        }

        private static void RegisterTheme(NoteTheme theme)
        {
            _themes[theme.Name] = theme;
        }

        public static NoteTheme GetTheme(string name)
        {
            if (string.IsNullOrEmpty(name)) return _themes["Yellow"];
            NoteTheme t;
            if (_themes.TryGetValue(name, out t))
            {
                return t;
            }
            return _themes["Yellow"];
        }

        public static IEnumerable<NoteTheme> AllThemes
        {
            get { return _themes.Values; }
        }

        private static SolidColorBrush CreateSolidBrush(string hex)
        {
            Color c = (Color)ColorConverter.ConvertFromString(hex);
            SolidColorBrush brush = new SolidColorBrush(c);
            brush.Freeze();
            return brush;
        }
    }
}

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace Taskora.Формы
{
    public static class Ui
    {
        public static readonly Color Background = ColorTranslator.FromHtml("#F5F5F5");
        public static readonly Color Panel = Color.White;
        public static readonly Color Primary = ColorTranslator.FromHtml("#6B46C1");
        public static readonly Color PrimaryLight = ColorTranslator.FromHtml("#EDE9FE");
        public static readonly Color Success = ColorTranslator.FromHtml("#10B981");
        public static readonly Color Danger = ColorTranslator.FromHtml("#EF4444");
        public static readonly Color Text = ColorTranslator.FromHtml("#1F2937");
        public static readonly Color Muted = ColorTranslator.FromHtml("#6B7280");
        public static readonly Color Border = ColorTranslator.FromHtml("#E5E7EB");
        public static readonly Color Warning = ColorTranslator.FromHtml("#D97706");

        private static readonly FontFamily AppFont = CreateFontFamily();

        public static Font GetFont(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(AppFont, size, style);
        }

        private static FontFamily CreateFontFamily()
        {
            try
            {
                return new FontFamily("Manrope");
            }
            catch
            {
                try
                {
                    return new FontFamily("Segoe UI");
                }
                catch
                {
                    return FontFamily.GenericSansSerif;
                }
            }
        }

        private static Image _logoCache;
        private static Icon _iconCache;

        public static Image LoadLogo()
        {
            if (_logoCache != null)
                return _logoCache;

            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("Taskora.logo.ico");

                if (stream == null)
                    return null;

                using var icon = new Icon(stream, 96, 96);
                _logoCache = icon.ToBitmap();
                return _logoCache;
            }
            catch
            {
                return null;
            }
        }

        public static Icon LoadIcon()
        {
            if (_iconCache != null)
                return _iconCache;

            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("Taskora.logo.ico");

                if (stream == null)
                    return null;

                _iconCache = new Icon(stream, 32, 32);
                return _iconCache;
            }
            catch
            {
                return null;
            }
        }

        public static Label CreateLabel(string text, float size, FontStyle style, Color color, bool autoSize = true)
        {
            return new Label
            {
                Text = text,
                Font = GetFont(size, style),
                ForeColor = color,
                AutoSize = autoSize
            };
        }

        public static TextBox CreateTextBox(int width = 260)
        {
            return new TextBox
            {
                Font = GetFont(11),
                Width = width,
                BackColor = Color.White,
                ForeColor = Text,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        public static ComboBox CreateCombo(int width = 260, bool editable = false)
        {
            return new ComboBox
            {
                Font = GetFont(11),
                Width = width,
                BackColor = Color.White,
                ForeColor = Text,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList
            };
        }

        public static Button CreateButton(string text, Color backColor, Size size)
        {
            return CreateButton(text, backColor, size, Color.White);
        }

        public static Button CreateButton(string text, Color backColor, Size size, Color foreColor)
        {
            Button button = new Button
            {
                Text = text,
                Size = size,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = GetFont(10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 0;

            return button;
        }

        public static Button CreateOutlineButton(string text, Color borderColor, Size size)
        {
            Button button = CreateButton(text, Color.White, size, Primary);

            button.FlatAppearance.BorderColor = borderColor;
            button.FlatAppearance.BorderSize = 1;

            return button;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Panel;
            grid.BorderStyle = BorderStyle.None;

            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 42;
            grid.RowTemplate.Height = 38;

            grid.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F9FAFB");
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
            grid.ColumnHeadersDefaultCellStyle.Font = GetFont(9, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#F9FAFB");

            grid.DefaultCellStyle.Font = GetFont(10);
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = PrimaryLight;
            grid.DefaultCellStyle.SelectionForeColor = Text;

            grid.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FCFCFC");

            grid.GridColor = Border;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        }

        public static GraphicsPath CreateRoundPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int r = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);

            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
            path.CloseFigure();

            return path;
        }
    }

    public class RoundedPanel : Panel
    {
        private int _radius = 16;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                UpdateRegion();
            }
        }

        public RoundedPanel()
        {
            DoubleBuffered = true;
            BackColor = Ui.Panel;
            UpdateRegion();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 2 || Height <= 2)
            {
                Region = null;
                return;
            }

            using GraphicsPath path = Ui.CreateRoundPath(new Rectangle(0, 0, Width - 1, Height - 1), _radius);
            Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (Width <= 2 || Height <= 2)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using GraphicsPath path = Ui.CreateRoundPath(new Rectangle(0, 0, Width - 1, Height - 1), _radius);
            using Pen border = new Pen(Ui.Border, 1f);
            e.Graphics.DrawPath(border, path);
        }
    }
}
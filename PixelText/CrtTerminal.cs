using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

namespace PixelText
{
    class CrtTerminal
    {
        private Color _FColor;
        private Color _BColor;
        private int _DotBlur;
        private Brush _FBrush;
        private Brush _FGlowBrush;
        private Brush _BBrush;
        private Bitmap _screen;
        private Graphics _gr;
        private char[,] _textBuffer;
        private Color[,] _colorBuffer;
        private int _columns;
        private int _rows;
        private bool[,,] _charset;
        private int _caretRow;
        private int _caretColumn;
        private PictureBox _dstImage;
        private Graphics _dstImageGraphics;
        private Timer cursorTimer;
        private object _lockScreenUpdate = new object();
        private int _lastRow;
        private int _lastColumn;
        private bool _showCursor;
        private DisplayType _displayType;

        public delegate void OnCursorPositionChangeHandler(int row, int column);
        public event OnCursorPositionChangeHandler OnCursorPositionChange;

        /// <summary>
        /// Default fore color for green crt
        /// </summary>
        public readonly Color InitFColorGreen = Color.FromArgb(255, 128, 255, 128);

        /// <summary>
        /// Default fore color for b/w crt
        /// </summary>
        public readonly Color InitFColorBW = Color.FromArgb(255, 194, 248, 255);

        /// <summary>
        /// Default background color for green crt
        /// </summary>
        public readonly Color InitBgColorGreen = Color.FromArgb(255, 5, 10, 5);

        /// <summary>
        /// Default background color for b/w crt
        /// </summary>
        public readonly Color InitBgColorBW = Color.FromArgb(255, 16, 18, 20); //255, 95, 107, 118

        /// <summary>
        /// Update screen constants. Described what to update
        /// </summary>
        public enum ScreenUpdateType
        {
            Char,
            Row,
            Full
        }

        /// <summary>
        /// Define display color modes
        /// </summary>
        public enum DisplayType
        {
            Green6105,
            Bw6105,
            Color6106
        }

        /// <summary>
        /// Type of the calculating cursor movement
        /// </summary>
        private enum CalculateCursorMovementType
        {
            Cursor,
            NewPlace
        }


        // dimension of the char in charset array
        private const int charWidth = 5;
        private const int charHeight = 7;

        // dimension of the char on the screen include white spaces border
        private const int scrCharWidth = charWidth + 1;
        private const int scrCharHeight = 2 * charHeight + 3;

        /// <summary>
        /// Set display color type
        /// </summary>
        public DisplayType displayType
        {
            get
            {
                return _displayType;
            }
            set
            {
                _displayType = value;
                switch(value)
                {
                    case DisplayType.Green6105:
                        FColor = InitFColorGreen;
                        BColor = InitBgColorGreen;
                        break;
                    case DisplayType.Bw6105:
                        FColor = InitFColorBW;
                        BColor = InitBgColorBW;
                        break;
                    case DisplayType.Color6106:
                        FColor = Color.White;
                        BColor = Color.Black;
                        break;
                }
                DrawCrtBuffer();
                UpdateScreen(ScreenUpdateType.Full);
            }
        }

        /// <summary>
        /// Show or hide cursor
        /// </summary>
        public bool ShowCursor
        {
            get => _showCursor;
            set
            {
                _showCursor = value;
                if (!_showCursor) _HideCursor();
            }
        }

        /// <summary>
        /// Get or set destination PictureBox component binded with terminal screen.
        /// While destination image is set, all output to the screen will redraw destination
        /// image immideately.
        /// </summary>
        public PictureBox destinationImage
        {
            get => _dstImage;
            set
            {
                _dstImage = value;
                _dstImageGraphics = Graphics.FromImage(value.Image);
            }
        }

        /// <summary>
        /// Get or set the background color
        /// </summary>
        public Color BColor
        {
            get => _BColor;
            set
            {
                _BBrush = new SolidBrush(value);
                _BColor = value;
            }
        }

        /// <summary>
        /// Get or set the pixel color
        /// </summary>
        public Color FColor
        {
            get => _FColor;
            set
            {
                _FColor = value;
                _FBrush = new SolidBrush(Color.FromArgb(255, value.R, value.G, value.B));
                _FGlowBrush = new SolidBrush(Color.FromArgb(_DotBlur, value.R, value.G, value.B));
            }
        }

        /// <summary>
        /// Get or set blurring of pixels
        /// </summary>
        public int DotBlur
        {
            get => _DotBlur;
            set
            {
                if (value > 255) value = 255;
                if (value < 0) value = 0;
                _DotBlur = value;
                _FGlowBrush = new SolidBrush(Color.FromArgb(_DotBlur, _FColor.R, _FColor.G, _FColor.B));
            }
        }

        /// <summary>
        /// Return number of columns of the terminal
        /// </summary>
        public int Columns
        {
            get => _columns;
        }

        /// <summary>
        /// Return number of rows of the terminal
        /// </summary>
        public int Rows
        {
            get => _rows;
        }

        /// <summary>
        /// Get or set cursor row position
        /// </summary>
        public int CaretRow
        {
            get => _caretRow;
            set
            {
                if (value < Rows && value >= 0)
                {
                    SetCursorPos(value, _caretColumn);
                }
            }
        }

        /// <summary>
        /// Get or set cursor column position
        /// </summary>
        public int CaretColumn
        {
            get => _caretColumn;
            set
            {
                if (value < Columns && value >= 0)
                {
                    SetCursorPos(_caretRow, value);
                }
            }
        }



        /// <summary>
        /// Costructor. Created the in-memory terminal windows with predefined width and height in symbols.
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="rows"></param>
        /// <param name="destinationImage"></param>
        public CrtTerminal(int columns, int rows, PictureBox destinationImage)
        {
            //BColor = Color.FromArgb(255, 50, 205, 50);
            _DotBlur = 30;
            FColor = InitFColorGreen;
            BColor = InitBgColorGreen;

            _columns = columns;
            _rows = rows;
            _lastColumn = _columns - 1;
            _lastRow = _rows - 1;
            _showCursor = true;

            destinationImage.Image = new Bitmap(destinationImage.ClientRectangle.Width, destinationImage.ClientRectangle.Height);
            this.destinationImage = destinationImage;
            // make PictureBox be focusable
            destinationImage.TabStop = true;
            typeof(PictureBox)
                .GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(destinationImage, new object[] { ControlStyles.Selectable, true });

            _screen = new Bitmap(destinationImage.Image.Width, destinationImage.Image.Height, PixelFormat.Format32bppArgb);
            _gr = Graphics.FromImage(_screen);
            _textBuffer = new char[rows, columns];
            _colorBuffer = new Color[rows, columns];
            ClearTextBuffer();
            ClearCrtScreen();

            LoadFonts();

            ShowCursor = true;

            cursorTimer = new System.Windows.Forms.Timer();
            cursorTimer.Interval = 500;
            cursorTimer.Tag = true;
            cursorTimer.Tick += CursorTimer_Tick;
            cursorTimer.Enabled = true;
        }

        /// <summary>
        /// Cursor blinkig routine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CursorTimer_Tick(object sender, EventArgs e)
        {
            lock (_lockScreenUpdate)
            {

                bool showCur = (bool)cursorTimer.Tag;

                if (ShowCursor)
                {
                    int x = _caretColumn * scrCharWidth;
                    int y = _caretRow * scrCharHeight;

                    if (showCur)
                        _ShowCursor();
                    else
                        _HideCursor();

                    if (_dstImage != null)
                    {
                        Rectangle dstRect = new Rectangle(x - 1, y - 1, charWidth + 2, scrCharHeight + 2);

                        _dstImageGraphics.DrawImage(
                            _screen,
                            dstRect,
                            x - 1, y - 1, charWidth + 2, scrCharHeight + 2,
                            GraphicsUnit.Pixel);

                        _dstImage.Invalidate(dstRect);
                    }
                }

                cursorTimer.Tag = !showCur;
            }
        }

        /// <summary>
        /// Show cursor image
        /// </summary>
        private void _ShowCursor()
        {
            if (!ShowCursor) return;

            int x = _caretColumn * scrCharWidth;
            int y = _caretRow * scrCharHeight;

            _gr.FillRectangle(_BBrush, x - 1, y - 1, charWidth + 2, scrCharHeight); // clear

            for (int r = 0; r < charHeight; r++)
                for (int c = 0; c < charWidth; c++)
                {
                    if (_charset[127 - 32, r, c])
                    {
                        // glow
                        _gr.FillRectangle(
                            _FGlowBrush,
                            x + c - 1,
                            y + r * 2 - 1,
                            3, 3);

                        // dot
                        _gr.FillRectangle(
                            _FBrush,
                            x + c,
                            y + r * 2,
                            1, 1);
                    }
                }

            cursorTimer.Tag = true;

            UpdateScreen(ScreenUpdateType.Char, _caretRow, _caretColumn);
        }

        /// <summary>
        /// Hide cursor image. If there is a symbol on the same place, it will be displayed.
        /// </summary>
        private void _HideCursor()
        {
            if (!ShowCursor) return;

            int x = _caretColumn * scrCharWidth;
            int y = _caretRow * scrCharHeight;

            _gr.FillRectangle(_BBrush, x - 1, y - 1, charWidth + 2, scrCharHeight); // clear
            char ch = _textBuffer[_caretRow, _caretColumn];

            if (ch > 31) // only visible symbols
                for (int r = 0; r < charHeight; r++)
                    for (int c = 0; c < charWidth; c++)
                    {
                        if (_charset[ch - 32, r, c])
                        {
                            // glow
                            _gr.FillRectangle(
                                _FGlowBrush,
                                x + c - 1,
                                y + r * 2 - 1,
                                3, 3);

                            // dot
                            _gr.FillRectangle(
                                _FBrush,
                                x + c,
                                y + r * 2,
                                1, 1);
                        }
                    }

            cursorTimer.Tag = false;

            UpdateScreen(ScreenUpdateType.Char, _caretRow, _caretColumn);
        }

        /// <summary>
        /// Clear text buffer
        /// </summary>
        private void ClearTextBuffer()
        {
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                {
                    _textBuffer[r, c] = '\0';
                    _colorBuffer[r, c] = FColor;
                }

            SetCursorPos(0, 0);;
        }

        /// <summary>
        /// Clear internal graphical screen
        /// </summary>
        private void ClearCrtScreen()
        {
            _gr.FillRectangle(_BBrush, 0, 0, _screen.Width, _screen.Height);
        }

        /// <summary>
        /// Load fonts from resource.
        /// </summary>
        private void LoadFonts()
        {
            _charset = new bool[96, charHeight, charWidth];
            Color pxBlack = Color.FromArgb(0, 0, 0);
            Color pxWhite = Color.FromArgb(255, 255, 255);

            //using (Bitmap b = new Bitmap(@"..\..\..\images\letters_16bit.bmp"))
            using (Bitmap b = new Bitmap(Properties.Resources.letters_16bit))
            {
                for (int c = 0; c < 96; c++)
                    for (int x = 0; x < charWidth; x++)
                        for (int y = 0; y < charHeight; y++)
                            if (b.GetPixel(c * 6 + x, y) == pxBlack)
                            {
                                _charset[c, y, x] = true;
                            }
                            else
                            {
                                _charset[c, y, x] = false;
                            }
            }
        }

        /// <summary>
        /// Draw symbol between 32 and 127 (inclusive) onto the screen and update text buffer (TB).
        /// If symbol equial zero or new line, it clears the symbol in position on screen and update TB
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        private void PutSymbolAt(char ch, int row, int col, Color color)
        {
            SolidBrush FBrush;// = new SolidBrush(color);
            SolidBrush FGlowBrush;// = new SolidBrush(Color.FromArgb(_DotBlur, color.R, color.G, color.B));

            switch(_displayType)
            {
                default:
                    FBrush = new SolidBrush(InitFColorGreen);
                    FGlowBrush = new SolidBrush(Color.FromArgb(_DotBlur, FBrush.Color.R, FBrush.Color.G, FBrush.Color.B));
                    break;
                case DisplayType.Bw6105:
                    FBrush = new SolidBrush(InitFColorBW);
                    FGlowBrush = new SolidBrush(Color.FromArgb(_DotBlur, FBrush.Color.R, FBrush.Color.G, FBrush.Color.B));
                    break;
                case DisplayType.Color6106:
                    FBrush = new SolidBrush(color);
                    FGlowBrush = new SolidBrush(Color.FromArgb(_DotBlur, color.R, color.G, color.B));
                    break;
            }

            int x = col * scrCharWidth;
            int y = row * scrCharHeight;

            if (ch == 0 || ch == '\n')
            {
                _textBuffer[row, col] = ch;
                _colorBuffer[row, col] = color;
                _gr.FillRectangle(_BBrush, x - 1, y - 1, charWidth + 2, scrCharHeight); // clear
            }
            else if (ch > 31 && ch < 128)
            {
                _textBuffer[row, col] = ch;
                _colorBuffer[row, col] = color;

                _gr.FillRectangle(_BBrush, x - 1, y - 1, charWidth + 2, scrCharHeight); // clear

                if (ch != 32)
                {
                    for (int r = 0; r < charHeight; r++)
                        for (int c = 0; c < charWidth; c++)
                        {
                            if (_charset[ch - 32, r, c])
                            {
                                // glow
                                _gr.FillRectangle(
                                    FGlowBrush,
                                    x + c - 1,
                                    y + r * 2 - 1,
                                    3, 3);

                                // dot
                                _gr.FillRectangle(
                                    FBrush,
                                    x + c,
                                    y + r * 2,
                                    1, 1);
                            }
                        }
                }
            }
        }

        /// <summary>
        /// Draw whole text buffer onto the screen
        /// </summary>
        public void DrawCrtBuffer()
        {
            ClearCrtScreen();
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                    if (_textBuffer[r, c] != '\0' && _textBuffer[r, c] != '\n')
                    {
                        PutSymbolAt(_textBuffer[r, c], r, c, _colorBuffer[r, c]);
                    }
        }

        /// <summary>
        /// Clear the screen
        /// </summary>
        public void ClearScreen()
        {
            lock (_lockScreenUpdate)
            {
                ClearTextBuffer();
                ClearCrtScreen();
                UpdateScreen(ScreenUpdateType.Full);
            }
        }

        /// <summary>
        /// Print text in the specified position.
        /// Text will be clipped if get out of the screen boundaries.
        /// Cursor position will not be moved.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="text"></param>
        public void PrintAt(int row, int column, string text, Color? color = null)
        {
            PrintAt(text, row, column, color);
        }

        /// <summary>
        /// Print text in the specified position.
        /// Text will be clipped if get out of the screen boundaries.
        /// Cursor position will not be moved.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public void PrintAt(string text, int row, int column, Color? color=null)
        {
            if (text == "") return;

            lock (_lockScreenUpdate)
            {
                _HideCursor();

                foreach (char c in text)
                {
                    if (row >= 0 && row < Rows && column >= 0 && column < Columns)
                    {
                        PutSymbolAt(c, row, column, color ?? FColor);
                    }

                    column++;
                }

                _ShowCursor();
                UpdateScreen(ScreenUpdateType.Row, row);
            }
        }

        /// <summary>
        /// Print the text at the current cursor position.
        /// Existing text on screen will be replaced.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Number of symbols printed</returns>
        public void Print(string text, Color? color = null)
        {
            if (text == "") return;

            lock (_lockScreenUpdate)
            {
                _HideCursor();

                foreach (char ch in text)
                {
                    // check if we reach end of the buffer
                    if (_caretColumn == _lastColumn && _caretRow == _lastRow
                        && _textBuffer[_caretRow, _caretColumn] != 0)
                    {
                        break;
                    }

                    // for all visible symbols
                    if (ch > 31)
                    {
                        PutSymbolAt(ch, _caretRow, _caretColumn, color ?? FColor);
                        UpdateScreen(ScreenUpdateType.Char, _caretRow, _caretColumn);

                        var newPos = CalculateNextCursorPositionRight(CalculateCursorMovementType.NewPlace);
                        SetCursorPos(newPos.Y, newPos.X);
                    }
                    else if (ch == '\n')
                    {
                        // check if the new string out of the screen
                        if (_caretRow == _lastRow) break;

                        // set to zero all rest of the row
                        for (int t = _caretColumn; t <= _lastColumn; t++) PutSymbolAt('\x0', _caretRow, t, color ?? FColor);
                        PutSymbolAt('\n', _caretRow, _caretColumn, color ?? FColor);
                        UpdateScreen(ScreenUpdateType.Row, _caretRow);

                        var newPos = CalculateNextCursorPositionRight(CalculateCursorMovementType.NewPlace);
                        SetCursorPos(newPos.Y, newPos.X);
                    }
                    else if (ch == 8)
                    {
                        // delete button
                        char clearSymbol = ' ';
                        if (_textBuffer[_caretRow, _caretColumn] < 32)
                        {
                            clearSymbol = '\x0';
                            PutSymbolAt(clearSymbol, _caretRow, _caretColumn, color ?? FColor);
                        }

                        var newPos = CalculateNextCursorPositionLeft();
                        SetCursorPos(newPos.Y, newPos.X);

                        PutSymbolAt(clearSymbol, _caretRow, _caretColumn, color ?? FColor);
                        UpdateScreen(ScreenUpdateType.Char, _caretRow, _caretColumn);
                    }
                }

                _ShowCursor();
            }
        }

        /// <summary>
        /// Free move cursor to the specified posions.
        /// Cursor coordinates will not be changed if the new position is out of the screen boundaries.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public void MoveCursorAt(int row, int column)
        {
            lock (_lockScreenUpdate)
            {
                _HideCursor();
                CaretRow = row;
                CaretColumn = column;
                _ShowCursor();
            }
        }

        /// <summary>
        /// Calculate position of cursor if the caret must go left,
        /// taking in account words in the text buffer.
        /// </summary>
        /// <returns></returns>
        private Point CalculateNextCursorPositionLeft()
        {
            int r = _caretRow;
            int c = _caretColumn;

            if (c > 0)
            {
                c--;
            }
            else
            {
                if (r > 0)
                {
                    r--;
                    c = Columns;
                    while (c > 0 && _textBuffer[r, c - 1] < 32) c--;
                    if (c == Columns) c--;
                }
            }

            return new Point()
            {
                X = c,
                Y = r
            };
        }

        /// <summary>
        /// Calculate position of cursor if the caret must go right,
        /// taking in account words in the text buffer.
        /// </summary>
        /// <param name="mt">Type of movement. For NewPlace it searches new available symbol to input.
        /// For Cursor it searches the last available position to move.</param>
        /// <returns></returns>
        private Point CalculateNextCursorPositionRight(CalculateCursorMovementType mt)
        {
            int r = _caretRow;
            int c = _caretColumn;

            if (c == _lastColumn && r == _lastRow)
            {

            }
            else if (c == _lastColumn || _textBuffer[r, c] == 0 || _textBuffer[r, c] == '\n')
            {
                if (r < _lastRow)
                {
                    if (mt == CalculateCursorMovementType.NewPlace)
                    {
                        r++;
                        c = 0;
                    }
                    else if (_textBuffer[r + 1, 0] != 0) // cursor movement
                    {
                        r++;
                        c = 0;
                    }
                }
            }
            else
            {
                c++;
            }

            return new Point()
            {
                X = c,
                Y = r
            };
        }

        /// <summary>
        /// Move cursor position to the one symbol left.
        /// </summary>
        public void MoveCursorLeft()
        {
            var newPos = CalculateNextCursorPositionLeft();
            MoveCursorAt(newPos.Y, newPos.X);
        }

        /// <summary>
        /// Move cursor position to the one symbol right.
        /// </summary>
        public void MoveCursorRight()
        {
            var newPos = CalculateNextCursorPositionRight(CalculateCursorMovementType.Cursor);
            MoveCursorAt(newPos.Y, newPos.X);
        }

        /// <summary>
        /// Return evaluated terminal width and heigth in symbols.
        /// </summary>
        /// <param name="pxWidth"></param>
        /// <param name="pxHeight"></param>
        /// <returns></returns>
        public static Size CalculateTerminalDimension(int pxWidth, int pxHeight)
        {
            return new Size()
            {
                Width = pxWidth / scrCharWidth,
                Height = pxHeight / scrCharHeight
            };
        }

        /// <summary>
        /// Set focus on the screen control
        /// </summary>
        public void SetFocus()
        {
            destinationImage.Focus();
        }

        /// <summary>
        /// Repaint PictureBox image with specified update type.
        /// </summary>
        /// <param name="sut"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void UpdateScreen(ScreenUpdateType sut = ScreenUpdateType.Full, int row=-1, int col=-1)
        {
            if (sut == ScreenUpdateType.Full)
            {
                _dstImageGraphics.DrawImage(_screen, 0, 0, _screen.Width, _screen.Height);
                _dstImage.Refresh();
            }
            else if (sut == ScreenUpdateType.Row)
            {
                Rectangle r = new Rectangle(
                        0, row * scrCharHeight - 1,
                        _screen.Width, scrCharHeight + 2
                    );
                _dstImageGraphics.DrawImage(_screen, r, r, GraphicsUnit.Pixel);
                _dstImage.Invalidate(r);
            }
            else if (sut == ScreenUpdateType.Char)
            {
                Rectangle r = new Rectangle(
                    col * scrCharWidth - 1 , row * scrCharHeight - 1,
                    _screen.Width + 2, scrCharHeight + 2
                );
                _dstImageGraphics.DrawImage(_screen, r, r, GraphicsUnit.Pixel);
                _dstImage.Invalidate(r);
            }
        }

        /// <summary>
        /// Set cursor position and file appropriate event
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        private void SetCursorPos(int row, int column)
        {
            _caretRow = row;
            _caretColumn = column;
            OnCursorPositionChange?.Invoke(row, column);
        }

        /// <summary>
        /// Return the text of the text buffer. '\n' symbols will be replaced with '\r\n'
        /// </summary>
        /// <returns></returns>
        public string GetText()
        {
            StringBuilder sb = new StringBuilder();

            //for (int h = 0; h < _rows; h++)
            //    for (int w = 0; w < _columns; w++)
            //        if (_textBuffer[h, w] != 0)
            //        {
            //            if (_textBuffer[h, w] == '\n')
            //                sb.Append("\r\n");
            //            else if (_textBuffer[h, w] > 31)
            //                sb.Append(_textBuffer[h, w]);
            //        }
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _columns; c++)
                {
                    if (_textBuffer[r, c] != 0)
                        sb.Append(_textBuffer[r, c]);
                    else
                        sb.Append(" ");
                }
                sb.Append("\r\n");
            }

            return sb.ToString();
        }
    }
}

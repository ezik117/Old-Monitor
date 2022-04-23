using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PixelText
{
    public partial class Form1 : Form
    {
        CrtTerminal crt;
        Bitmap knob_orig;
        bool gameMode = false;

        Game2 game2;

        public Form1()
        {
            InitializeComponent();

            knob_orig = new Bitmap(knobGlow.Image);
            using (Graphics g = Graphics.FromImage(knobGlow.Image))
                g.DrawImage(knobGlow.Image, 0, 0);

            CreateNewCrt();
        }

        private void CreateNewCrt(int rows=24, int cols=100)
        {
            crt = new CrtTerminal(cols, rows, pictureBox1);
            crt.OnCursorPositionChange += Crt_OnCursorPositionChange;
            crt.ClearScreen();
            pictureBox1.Focus();

            pictureBox1.MouseEnter += Knob_MouseEnter;
            pictureBox1.PreviewKeyDown += PictureBox1_PreviewKeyDown;

            knobGlow.MouseWheel += KnobGlow_MouseWheel;
            knobGlow.MouseEnter += Knob_MouseEnter;

            knobBright.MouseWheel += KnobBright_MouseWheel;
            knobBright.MouseEnter += Knob_MouseEnter;

            lblGlowVal.Text = crt.DotBlur.ToString();
            lblBright.Text = crt.BColor.G.ToString();

            knobBright.Tag = 0;
            knobGlow.Tag = 0;

            knobBright.Image = knob_orig;
            knobGlow.Image = knob_orig;

            crt.Print($"< Current console resolution: {crt.Rows} x {crt.Columns} >\n\n", Color.Pink);
            crt.Print("Welcome to the old style CRT monitor emulator.\nPlease type anyting in English only.\n", Color.White);
            crt.Print("You can apply some functionalties from the right panel.\n\n", Color.White);
            crt.Print("Some tips:\n", Color.Coral);
            crt.Print("   - Put cursor over the CRT screen to enable keyboard input\n", Color.White);
            crt.Print("   - Press 'CLR' to clear the screen\n", Color.White);
            crt.Print("   - Change window size and press 'Create new terminal' to set console widht equal the window\n", Color.White);
            crt.Print("   - Hit CTRL-V to paste a content into the console\n", Color.White);
            crt.Print("   - Move the mouse pointer over the knobs and rotate the wheel to ajust screen properties\n", Color.White);
            crt.Print("   - Press 'Play the game' to play an old fashioned game\n", Color.Magenta);

            crt.Print("   - You ", Color.White);
            crt.Print("can even ", Color.White);
            crt.Print("colorize ", Color.Cyan);
            crt.Print("the text ", Color.Yellow);
            crt.Print("if you wish\n", Color.Pink);
            crt.Print("\n");
         }



        private void KnobBright_MouseWheel(object sender, MouseEventArgs e)
        {
            Color c = crt.BColor;
            int delta = 1;
            int rotate = (int)knobBright.Tag;

            if (e.Delta < 0)
            {
                rotate += 5;
                if (rotate > 360) rotate = 5;
                knobBright.Image = RotateImage(knob_orig, rotate);
                if (crt.BColor.R < 128 && crt.BColor.G < 128 && crt.BColor.B < 128)
                {
                    if (crt.displayType == CrtTerminal.DisplayType.Green6105)
                        crt.BColor = Color.FromArgb(c.R + delta, c.G + delta*2, c.B + delta);
                    else
                        crt.BColor = Color.FromArgb(c.R + delta, c.G + delta, c.B + delta);
                }
            }
            else
            {
                rotate -= 5;
                if (rotate < 0) rotate = -5;
                knobBright.Image = RotateImage(knob_orig, rotate);
                if (crt.BColor.R > 0 && crt.BColor.G > 0 && crt.BColor.B > 0)
                {
                    if (crt.displayType == CrtTerminal.DisplayType.Green6105)
                        crt.BColor = Color.FromArgb(c.R - delta, c.G - delta*2, c.B - delta);
                    else
                        crt.BColor = Color.FromArgb(c.R - delta, c.G - delta, c.B - delta);
                }
            }

            knobBright.Refresh();
            lblBright.Text = c.G.ToString();
            knobBright.Tag = rotate;
            crt.DrawCrtBuffer();
            crt.UpdateScreen(CrtTerminal.ScreenUpdateType.Full);

            Debug.WriteLine(crt.BColor);

        }

        private void Knob_MouseEnter(object sender, EventArgs e)
        {
            ((PictureBox)sender).Focus();
        }

        private void KnobGlow_MouseWheel(object sender, MouseEventArgs e)
        {
            int rotate = (int)knobGlow.Tag;

            if (e.Delta < 0)
            {
                if (crt.DotBlur < 255)
                {
                    rotate += 5;
                    if (rotate > 360) rotate = 5;
                    knobGlow.Image = RotateImage(knob_orig, rotate);
                    crt.DotBlur++;
                }
            }
            else
            {
                if (crt.DotBlur > 0)
                {
                    rotate -= 5;
                    if (rotate < -360) rotate = 0;
                    knobGlow.Image = RotateImage(knob_orig, rotate);
                    crt.DotBlur--;
                }
            }

            knobGlow.Refresh();
            knobGlow.Tag = rotate;
            lblGlowVal.Text = crt.DotBlur.ToString();
            crt.DrawCrtBuffer();
            crt.UpdateScreen(CrtTerminal.ScreenUpdateType.Full);

            //Debug.WriteLine(crt.BColor);
        }

        public static Image RotateImage(Image img, float rotationAngle)
        {
            //create an empty Bitmap image
            Bitmap bmp = new Bitmap(img.Width, img.Height);
            bmp.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            //turn the Bitmap into a Graphics object
            Graphics gfx = Graphics.FromImage(bmp);

            //now we set the rotation point to the center of our image
            gfx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);

            //now rotate the image
            gfx.RotateTransform(rotationAngle);

            gfx.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);

            //set the InterpolationMode to HighQualityBicubic so to ensure a high
            //quality image once it is transformed to the specified size
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;

            //now draw our new image onto the graphics object
            gfx.DrawImage(img, new Point(0, 0));

            //dispose of our Graphics object
            gfx.Dispose();

            //return the image
            return bmp;
        }

        private void Crt_OnCursorPositionChange(int row, int column)
        {
            toolStripStatusLabel1.Text = $"X:{column} Y:{row} Resolution: {crt.Rows} rows x {crt.Columns} columns";
        }
  

        private void button2_Click(object sender, EventArgs e)
        {
            crt.ClearScreen();

            pictureBox1.Focus();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = crt.FColor;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                crt.FColor = cd.Color;
            }

            pictureBox1.Focus();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            pictureBox1.Focus();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var tw = CrtTerminal.CalculateTerminalDimension(pictureBox1.ClientRectangle.Width, pictureBox1.ClientRectangle.Height);
            CreateNewCrt(tw.Height, tw.Width);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            crt.MoveCursorAt(Convert.ToInt16(tbY.Text), Convert.ToInt16(tbX.Text));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            crt.PrintAt(tbText.Text, Convert.ToInt16(tbY.Text), Convert.ToInt16(tbX.Text));
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(crt.GetText());
            MessageBox.Show("Screen was copied to clipboard");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (crt.Rows < 24 || crt.Columns < 100)
            {
                MessageBox.Show("Console must be have at least 24 rows x 100 columns to play the game.");
                return;
            }

            //crt.BColor = Color.FromArgb(13, 26, 13);
            //crt.BColor = Color.FromArgb(24, 28, 31); // 95,107,118
            //crt.FColor = crt.InitFColor;
            //crt.FColor = Color.FromArgb(194, 248, 255);
            //lblBright.Text = "26";

            if (game2 != null) return;

            crt.ClearScreen();
            crt.ShowCursor = false;

            gameMode = true;

            game2 = new Game2(crt, this);
            game2.Start();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            //if (pictureBox1.Focused && gameMode)
            //{
            //    if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            //        game.man.SetAction(Game.GameActionTypes.Stop);
            //}
        }

        private void PictureBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (pictureBox1.Focused)
            {
                if (!gameMode)
                {
                    if (e.KeyCode == Keys.Left)
                        crt.MoveCursorLeft();
                    else if (e.KeyCode == Keys.Right)
                        crt.MoveCursorRight();
                    else if (e.Control && e.KeyCode == Keys.V)
                    {
                        crt.Print(Clipboard.GetText());
                    }
                }
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (pictureBox1.Focused && !gameMode)
            {
                if (e.KeyChar > 31)
                {
                    crt.Print(e.KeyChar.ToString());
                    e.Handled = true;
                }
                else if (e.KeyChar == 13)
                {
                    crt.Print("\n");
                    e.Handled = true;
                }
                else if (e.KeyChar == 8)
                {
                    crt.Print(e.KeyChar.ToString());
                    e.Handled = true;
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
 
        }

        private void MonitorType_CheckedChanged(object sender, EventArgs e)
        {
            switch (((RadioButton)sender).Name)
            {
                case "MonitorTypeGreen":
                    crt.displayType = CrtTerminal.DisplayType.Green6105;
                    break;
                case "MonitorTypeBW":
                    crt.displayType = CrtTerminal.DisplayType.Bw6105;
                    break;
                case "MonitorTypeColor":
                    crt.displayType = CrtTerminal.DisplayType.Color6106;
                    break;
            }

            crt.SetFocus();
        }
    }
}

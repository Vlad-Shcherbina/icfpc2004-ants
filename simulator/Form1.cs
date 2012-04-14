using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace simulator
{
    public partial class Form1 : Form
    {
        static int CELL_SIZE = 12;
        private Map map;
        private Simulator simulator;

        private Image image;

        public Form1(string name,Simulator sim)
        {
            simulator = sim;
            map = sim.map;
            InitializeComponent();

            this.Text = name;

            pictureBox1.Width = CELL_SIZE * (map.width - map.height / 2) + CELL_SIZE / 2;
            pictureBox1.Height = CELL_SIZE * map.height;

            image = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            render();
        }

        void render()
        {
            Graphics g = Graphics.FromImage(image);

            SolidBrush brush = new SolidBrush(Color.Green);
            Pen pen = new Pen(Color.White);

            for (int i = 0; i < map.width; i++)
                for (int j = 0; j < map.height; j++)
                {
                    int x,y;
                    toScreenCoords(i, j, out x, out y);
                    Cell cell = map.cells[i, j];
                    if (!cell.updated)
                        continue;

                    if (cell.food<10)
                        cell.updated = false;

                    brush.Color = Color.Green;
                    g.FillRectangle(brush,x, y, CELL_SIZE, CELL_SIZE);

                    pen.Color = Color.FromArgb(0, 120, 0);
                    g.DrawRectangle(pen,
                        new Rectangle(x, y, CELL_SIZE, CELL_SIZE));

                    if (cell.type == CellType.ROCK)
                    {
                        brush.Color = Color.Black;
                        g.FillRectangle(brush,
                            new Rectangle(x + 1, y + 1, CELL_SIZE - 1, CELL_SIZE - 1));
                    }
                    else if (cell.type == CellType.EMPTY)
                    {
                    }
                    else if (cell.type == CellType.RED_ANTHILL)
                    {
                        pen.Color = Color.Red;
                        g.DrawRectangle(pen, x + 1, y + 1, CELL_SIZE - 2, CELL_SIZE - 2);
                    }
                    else if (cell.type == CellType.BLACK_ANTHILL)
                    {
                        pen.Color = Color.Black;
                        g.DrawRectangle(pen, x + 1, y + 1, CELL_SIZE - 2, CELL_SIZE - 2);
                    }

                    if ((cell.markers[0] & 7) > 0)
                    {
                        int m = cell.markers[0];
                        pen.Color = Color.FromArgb((m & 1) * 155, ((m >> 1) & 1) * 155, ((m >> 2) & 1) * 155);
                        g.DrawRectangle(pen, x + 3, y + 3, 1, 1);
                    }
                    if ((cell.markers[0] & (7*8)) > 0)
                    {
                        int m = cell.markers[0];
                        pen.Color = Color.FromArgb(((m >> 3) & 1) * 155, ((m >> 4) & 1) * 155, ((m >> 5) & 1) * 155);
                        g.DrawRectangle(pen, x + 6, y + 6, 1, 1);
                    }

                    for (int k = 0; k < cell.food; k++)
                    {
                        if (k % 2 == 0)
                            pen.Color = Color.Yellow;
                        else
                            pen.Color = Color.Orange;
                        g.DrawRectangle(pen,
                            x + CELL_SIZE / 3 * (k % 3) + 1, y + CELL_SIZE / 3 * (k / 3 % 3) + 1 - k / 9,
                            CELL_SIZE / 3 - 2, CELL_SIZE / 3 - 2);
                    }

                    if (cell.ant != null)
                        drawAnt(cell.ant, g);
                }
        }

        private void drawAnt(Ant ant, Graphics g)
        {
            int x, y;
            toScreenCoords(ant.x, ant.y, out x, out y);
            SolidBrush brush = new SolidBrush(new Color[] { Color.DarkRed, Color.Black }[ant.color]);
            Matrix m = new Matrix();
            m.Translate(x + CELL_SIZE / 2, y + CELL_SIZE / 2);
            m.Rotate(60 * ant.direction);
            g.Transform = m;
            g.FillEllipse(brush,
                -5, -3, 10, 6);
            if (ant.hasFood)
            {
                Pen pen = new Pen(Color.Yellow);
                g.DrawRectangle(pen, 2, -1, 2, 2);
            }
            g.ResetTransform();
        }

        private void toScreenCoords(int i, int j, out int x, out int y)
        {
            x = i * CELL_SIZE - j * CELL_SIZE / 2;
            y = j * CELL_SIZE;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(image,0,0);
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            int x = e.X - panel1.Width / 2;
            int y = e.Y - panel1.Height / 2;
            if (x > panel1.HorizontalScroll.Maximum) x = panel1.HorizontalScroll.Maximum;
            if (x < 0) x = 0;
            if (y > panel1.VerticalScroll.Maximum) y = panel1.VerticalScroll.Maximum;
            if (y < 0) y = 0;

            panel1.HorizontalScroll.Value = x;
            panel1.VerticalScroll.Value = y;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (play)
            {
                for (int i = 0; i < trackBar1.Value; i++)
                    simulator.step();
                render();
                pictureBox1.Invalidate();
            }
        }

        bool play=false;

        private void startStopButton_Click(object sender, EventArgs e)
        {
            play = !play;
            if (play)
                startStopButton.Text = "&Stop";
            else
                startStopButton.Text = "&Start";
        }

        private void stepButton_Click(object sender, EventArgs e)
        {
            simulator.step();
            render();
            pictureBox1.Invalidate();
        }

        private void visibilityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Visible = visibilityCheckBox.Checked;
        }
    }
}

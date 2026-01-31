using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IdealGasSimulator
{
    public partial class StatisticsForm : Form
    {
        Bitmap BM;                                      //  Объект для хранения рисунка
        Graphics Gr;                                    //  Объект-инструмент для рисования на BM
        Pen Pen1 = new Pen(Color.Red, 2);               //  Карандаш красного цвета для объекта Gr
        Pen Pen2 = new Pen(Color.Blue, 1);              //  Карандаш синего цвета для объекта Gr  
        SolidBrush Br = new SolidBrush(Color.Yellow);   //  Кисть желтого цвета для объекта Gr
        double Kx;
        double Ky;
        double K;
        int NN;

        public StatisticsForm()
        {
            InitializeComponent();
        }

        //  Процедура выполняется при загрузке второй формы StatisticsForm
        private void Form2_Load(object sender, EventArgs e)
        {
            // Создание объектов для графики. Размер рисунка в BM такой же, как и в pictureBox1
            BM = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Gr = Graphics.FromImage(BM);
            K = 1;
            NN = 0;
        }


        // Процедура выполняется при закрытии формы StatisticsForm 
        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            MainForm.Fr1.посмотретьToolStripMenuItem.Enabled = true;  //  Разблокировать кнопку "Посмотреть" в меню первой формы
            MainForm.Fr2.Dispose();       //  Уничтожить объект Fr2. Он будет заново создан при необходимости работы с StatisticsForm.
        }

        public void DrawPicture()
        {
            int X1scr, Y1scr, X2scr, Y2scr;
            double x, y;         
            int Max = MainForm.CV[0];
            NN = 0;
            for (int i = 0; i < MainForm.NV; i++)
            {
                NN += MainForm.CV[i];
                if (MainForm.CV[i] > Max) Max = MainForm.CV[i];
            }

            
            textBox5.Text = NN.ToString();
            if (NN == 0) return;

            Kx = K * (32 * BM.Width / (MainForm.dV * MainForm.NV)) * Math.Pow(1.5, trackBar1.Value);
            Ky = K * (BM.Height / Max) * Math.Pow(1.5, trackBar2.Value);

            Gr.Clear(pictureBox1.BackColor);
            for (int i = 0; i < MainForm.NV; i++)
            {
                Gr.FillRectangle(Br, (int)(Kx * i * MainForm.dV), (int)(BM.Height - Ky * MainForm.CV[i]), (int)((Kx * MainForm.dV)), (int)(Ky * MainForm.CV[i]));
                Gr.DrawRectangle(Pen2, (int)(Kx * i * MainForm.dV), (int)(BM.Height - Ky * MainForm.CV[i]), (int)((Kx * MainForm.dV) + 1), (int)(Ky * MainForm.CV[i]));
            }

            X1scr = 0;
            Y1scr = BM.Height;
            for (int i = 1; i < BM.Width; i++)
            {
                X2scr = i;
                x = X2scr / Kx;
                y = (NN * MainForm.dV) * 2 * x * Math.Exp(-(x * x) / MainForm.V2sr) / MainForm.V2sr;
                Y2scr = (int)(BM.Height - Ky * y);
                Gr.DrawLine(Pen1, X1scr, Y1scr, X2scr, Y2scr);
                X1scr = X2scr;
                Y1scr = Y2scr;
            }

            pictureBox1.Image = BM;

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            DrawPicture();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            DrawPicture();
        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            if ((pictureBox1.Width == 0) || (pictureBox1.Height == 0)) return;
            BM = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Gr = Graphics.FromImage(BM);
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DrawPicture();
            timer1.Enabled = false;
        }

        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            K *= Math.Pow(1.1, e.Delta / 100);
            DrawPicture();
            pictureBox1_MouseMove(sender, e);
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            label6.Visible = true;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            label6.Visible = false;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (NN == 0) { label6.Text = ""; return; }
            label6.Left = pictureBox1.Left + e.X + 10;
            label6.Top = pictureBox1.Top + e.Y + 10;
            int index = (int)(e.X / Kx / MainForm.dV);
            if (index < MainForm.NV) label6.Text = MainForm.CV[index].ToString(); else label6.Text = "";
        }

      
    }
}




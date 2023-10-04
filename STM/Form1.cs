using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace STM
{
    public partial class Form1 : Form
    {
        public static int[] place = new int[16] { 0, 0, 10, 0, 10, 5, 20, 5, 20, 0, 25, 0, 25, 7, 35, 7};
        
        RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        public static readonly int N = 1000;

        public static byte[] rngBytes = new byte[2];

        public static double u = 0.1, ef = 5.71, fi = 4.5, k = 1;

        public static double[] z0 = new double[4] { 5, 7, 10, 15 };

        public double I_et = 0;

        public double delta = 0.02, EPS = 0.001;

        public int line_count;

        public double rangeY_integral = 10, rangeX_integral = 10;

        public Form1()
        {
            InitializeComponent();
        }

        public struct Line
        {
            public double dx, dy, x0, y0, x1, y1, Length, nX, nY;
        }

        public Line[] Line_ = new Line[11];

        public void SetLine(double x0, double y0, double x1, double y1, int counter)
        {
            Line_[counter].dx = x1 - x0;
            Line_[counter].dy = y1 - y0;
            Line_[counter].x0 = x0;
            Line_[counter].x1 = x1;
            Line_[counter].y0 = y0;
            Line_[counter].Length = Math.Sqrt(Line_[counter].dx * Line_[counter].dx + Line_[counter].dy * Line_[counter].dy);
            Line_[counter].dx /= Line_[counter].Length;
            Line_[counter].dy /= Line_[counter].Length;
            Line_[counter].nX = -Line_[counter].dy;
            Line_[counter].nY = Line_[counter].dx;
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        public double J(double x, double y, double z) {
            double zxy, s1, s2, fi_s;
            zxy = Math.Sqrt(x * x + y * y + z * z);
            s1 = 3 / (k * fi);
            s2 = zxy * (1 - 23 / (3 * fi * k * zxy + 10 - 2 * u * k * zxy)) + s1;
            fi_s = fi - (u * (s1 + s2) / (2 * zxy))
                - (2.86 / (k * (s2 - s1)))
                * Math.Log(s2 * (zxy - s2) / (s1 * (zxy - s2)));
            return 1620 * u * ef * Math.Exp(-1.025 * zxy * Math.Sqrt(fi_s));
        }

        public double Integrals(double xi, double yi) {
            double sum = 0;
            double z, x1, x2;
            for (int i = 0; i < line_count; i++) 
            {
                double d = (xi - Line_[i].x0) * Line_[i].nX + (yi - Line_[i].y0) * Line_[i].nY;
                if (d > 0)
                {
                    z = ((xi - d * Line_[i].nX) - Line_[i].x0) * Line_[i].dx + ((yi - d * Line_[i].nY) - Line_[i].y0) * Line_[i].dy;
                    if (z + rangeX_integral > Line_[i].Length) x1 = Line_[i].Length;
                    else x1 = z + rangeX_integral;
                    x2 = (z - rangeX_integral > 0) ? z - rangeX_integral : 0;
                    x1 -= z;
                    x2 -= z;
                    if (x2 < x1) sum += GetIntegral(d, x2, x1);
                    //if(line_count > 1)Console.WriteLine($"Ток {i} -й линии: {GetIntegral(d, x2, x1)}");
                }

            }
            return sum;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        public double GetIntegral(double z, double x_min, double x_max) {
            double sum = 0, x, y, rng_coef;
            int errorCounter = 0;
            for (int i = 0; i < N; i++) {
                rng.GetBytes(rngBytes);
                rng_coef = rngBytes[0];
                rng_coef /= 255;
                x = x_min + (x_max - x_min) * rng_coef;
                rng_coef = rngBytes[1];
                rng_coef /= 255;
                y = rangeY_integral * rng_coef;
                if (x < x_min || x > x_max) errorCounter++;
                else
                {
                    sum += J(x, y, z);
                }
            }
            return sum* (x_max - x_min) * rangeY_integral / N;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            chart1.Series[1].Points.Clear();
            chart1.Series[2].Points.Clear();
            chart1.Series[3].Points.Clear();
            chart1.Series[4].Points.Clear();
            button1.Enabled = false;
            if (comboBox1.SelectedIndex == 0) u = 0.01;
            if (comboBox1.SelectedIndex == 1) u = 0.1;
            double b = 35, h = 0.5;
            double delt = 0.01; 
            this.chart1.Invoke((MethodInvoker)delegate 
            {
                Profilogram1(b, z0[0], Integrals(0, z0[0]), delt, h, 1);
            });
        }


        public async void Profilogram1(double b, double zh, double I_et, double delt, double h, int index) {
            double Ik, zh2 = zh, Ik2;
            byte sign;
            button1.Enabled = false;
            button2.Enabled = false;
            for (double i = 0; i < b; i += h)
            {
                Ik = Integrals(i, zh2);
                if (Ik > I_et)
                {
                    zh2 = zh + delt;
                    sign = 0;
                }
                else
                {
                    zh2 = zh - delt;
                    sign = 1;
                }
                Ik2 = Integrals(i, zh2);
                if (Ik2 > I_et && sign == 0)
                {
                    while(Integrals(i, zh2)> I_et) zh2 = zh2 + delt;
                    //zh2 = zh2 + (Ik2 - I_et) * (zh2 - zh) / (Ik2 - Ik);
                    //Ik2 = Integrals(i, zh2);
                }
                else if (Ik2 < I_et && sign == 1)
                {
                    while (Integrals(i, zh2) < I_et) zh2 = zh2 - delt;
                    //zh2 = zh2 + (Ik2 - I_et) * (zh2 - zh) / (Ik2 - Ik);
                    //Ik2 = Integrals(i, zh2);
                }
                chart1.Series[index].Points.AddXY(i, zh2);
                zh = zh2;
                await Task.Delay(1);
            }
            if (index < 4)
            {
                this.chart1.Invoke((MethodInvoker)delegate
                {
                    Profilogram1(b, z0[index], Integrals(0, z0[index]), delt, h, index + 1);
                });
            }
            if (index == 3)
            {
                button1.Enabled = true;
                button2.Enabled = true;
            }

        }


        public void Form1_Load(object sender, EventArgs e)
        {
            this.chart1.Series[0].BorderWidth = 3;
            this.chart1.ChartAreas[0].AxisY.Maximum = 30;
            this.chart1.Series[0].Color = Color.Black;

            chart1.Series[1].BorderWidth = 2;
            chart1.Series[1].Color = Color.Black;
            chart1.Series[2].BorderWidth = 2;
            chart1.Series[2].Color = Color.Black;
            chart1.Series[3].BorderWidth = 2;
            chart1.Series[3].Color = Color.Black;
            chart1.Series[4].BorderWidth = 2;
            chart1.Series[4].Color = Color.Black;
            chart1.Series[0].Name = "Поверхность";
            chart1.Series[1].Name = "5A";
            chart1.Series[2].Name = "7A";
            chart1.Series[3].Name = "10A";
            chart1.Series[4].Name = "15A";

            comboBox1.Items.Add("0.01");
            comboBox1.Items.Add("0.1");

            for (int i = 0; i < 8; i++)
            {
                this.chart1.Series[0].Points.AddXY(place[i * 2], place[i * 2 + 1]);
                if (i > 0) SetLine(place[(i - 1) * 2], place[(i - 1) * 2 + 1], place[i * 2], place[i * 2 + 1], i - 1);
                line_count++;
            }
        }
    }
}

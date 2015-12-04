using System;
using System.Windows.Forms;

namespace ConvexHullCircles
{
    public partial class Form1 : Form
    {
        private Form2 form2 = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void buttonDraw_Click(object sender, EventArgs e)
        {
            this.labelResult.Text = "";
            if (form2 != null)
                form2.Close();
            form2 = new Form2(this);
            form2.Show();
        }

        private void Form1_DoubleClick(object sender, EventArgs e)
        {
            this.numericUpDownNrCircles.Value = 9;
            this.numericUpDownRadius.Value = 5;
            this.textBoxCoordinates.Text = "30 30\r\n10 10\r\n50 25\r\n10 25\r\n30 50\r\n50 10\r\n60 50\r\n30 80\r\n10 40\r\n";
            this.textBoxPointX.Text = "27";
            this.textBoxPointY.Text = "7";
        }
    }
}

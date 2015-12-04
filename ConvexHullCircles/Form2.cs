using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ConvexHullCircles
{
    public partial class Form2 : Form
    {
        const int SCALE = 5;
        const float POINTDIM = 1.5F;
        const float EPS = 0.0000001F;
        float radius = 5;
        Graphics g;
        Pen pen;
        Brush brush, brush2;
        Color lineColor;
        Form1 mainForm;

        public Form2()
        {
            lineColor = Color.FromArgb(231, 233, 104);
            pen = new Pen(lineColor, 1);
            brush = new SolidBrush(lineColor);
            brush2 = new SolidBrush(Color.Red);

            InitializeComponent();
        }

        public Form2(Form callingForm)
        {
            mainForm = callingForm as Form1;
            lineColor = Color.FromArgb(231, 233, 104);
            pen = new Pen(lineColor, 1);
            brush = new SolidBrush(lineColor);
            brush2 = new SolidBrush(Color.Red);

            InitializeComponent();
        }

        private void Form2_Paint(object sender, PaintEventArgs e)
        {
            int nrCircles = 0, stackSize = 0;
            Punct[] P, T;
            Stack<Punct> St = new Stack<Punct>();
            Punct M, G;
            g = e.Graphics;
            char[] delimiterChars = { ' ', '\t', '\n' };
            try
            {
                float x, y;
                float minX, minY, maxX, maxY;
                minX = minY = float.PositiveInfinity;
                maxX = maxY = float.NegativeInfinity;

                nrCircles = (int)mainForm.numericUpDownNrCircles.Value; // numarul de cercuri
                radius = (float)mainForm.numericUpDownRadius.Value; // raza cercurilor
                P = new Punct[nrCircles]; // centrele cercurilor
                G = new Punct(float.PositiveInfinity, float.PositiveInfinity); // pivotul pentru sortarea din Graham's Scan

                string buffer = mainForm.textBoxCoordinates.Text;
                string[] numbers = buffer.Split(delimiterChars);

                if (numbers.Length < 2 * nrCircles)
                    throw new Exception("Wrong input!");

                for (int i = 0; i < nrCircles; ++i)
                {
                    x = float.Parse(numbers[2 * i]);
                    y = float.Parse(numbers[2 * i + 1]);
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, -1 * y);
                    maxY = Math.Max(maxY, -1 * y);
                    P[i] = new Punct(x, y); // centrul cercului curent
                    if (y < G.y || (Math.Abs(y - G.y) < EPS && x < G.x))
                    {
                        G.x = x;
                        G.y = y;
                    }
                }

                x = float.Parse(mainForm.textBoxPointX.Text);
                y = float.Parse(mainForm.textBoxPointY.Text);
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, -1 * y);
                maxY = Math.Max(maxY, -1 * y);
                M = new Punct(x, y); // punctul caruia ii trebuie stabilita pozitia relativa fata de multimea convexa a reuniunii cercurilor

                g.ScaleTransform(SCALE, SCALE);
                g.TranslateTransform(-minX + radius * 1.5F, -minY + radius * 1.5F);
                this.Width = SCALE * (int)Math.Floor(maxX - minX + 3 * radius) + 50;
                this.Height = SCALE * (int)Math.Floor(maxY - minY + 3 * radius) + 50;

                Array.Sort(P, delegate (Punct A, Punct B) // sortez punctele dupa unghiul si distanta polara fata de G
                {
                    if (Math.Abs((A.x - G.x) * (B.y - G.y) - (B.x - G.x) * (A.y - G.y)) < EPS)
                        return (Distance(A, G) < Distance(B, G) ? 1 : -1);
                    return ((A.x - G.x) * (B.y - G.y) < (B.x - G.x) * (A.y - G.y) ? 1 : -1);
                });

                for (int i = 0; i < nrCircles; ++i) // Graham's Scan
                {
                    while (stackSize >= 2)
                    {
                        Punct A = St.Pop();
                        Punct B = St.Pop();
                        if (isLeftTurn(B, A, P[i]))
                        {
                            St.Push(B);
                            St.Push(A);
                            break;
                        }
                        else
                        {
                            St.Push(B);
                            stackSize--;
                        }
                    }
                    St.Push(P[i]);
                    stackSize++;
                }

                T = DrawTangents(St.ToArray()); // determin si trasez tangentele ce completeaza frontiera acoperiririi convexe
                DrawCircles(P); // desenez toate cercurile
                DrawPoints(P, M); // desenez centrele cercurilor si punctul M
                DeterminePointPosition(St.ToArray(), T, M); // determin pozitia punctului M fata de acoperirea convexa a reuniunii cercurilor
            }
            catch (Exception error)
            {
                this.Close();
                MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeterminePointPosition(Punct[] P, Punct[] T, Punct M)
        {
            int nrCircles = P.Length, nrPoints = T.Length, position = -1;
            bool sameSide = true;
            if (nrCircles == 1) // acoperirea convexa e doar discul cu centrul in P[0]
            {
                float d = Distance(M, P[0]);
                if (d - radius > EPS)
                    position = -1;
                else
                {
                    if (d - radius < -EPS)
                        position = 1;
                    else
                        position = 0;
                }
            }
            else
            {
                for (int i = 0; i < nrPoints - 1 && sameSide; ++i)
                {
                    if (isRightTurn(T[i], T[i + 1], M))
                        sameSide = false;
                }
                if (nrPoints > 1 && isRightTurn(T[nrPoints - 1], T[0], M))
                    sameSide = false;
                if (sameSide) // este in interiorul poligonului convex determinat de punctele de tangenta
                {
                    position = 1;
                    for (int i = 0; i < nrPoints - 1; ++i)
                    {
                        if (!isLeftTurn(T[i], T[i + 1], M))
                            position = 0;
                    }
                    if (nrPoints > 1 && !isLeftTurn(T[nrPoints - 1], T[0], M))
                        position = 0;
                }
                else // ca sa nu fie in exterior mai e doar cazul in care se afla in vreun disc
                {
                    for (int i = 0; i < nrCircles; ++i)
                        position = Math.Max(position, InsideCircle(P[i], M));
                }
            }
            switch (position)
            {
                case -1:
                    mainForm.labelResult.Text = "Punctul este in exteriorul acoperirii convexe";
                    break;
                case 0:
                    mainForm.labelResult.Text = "Punctul este pe frontiera acoperirii convexe";
                    break;
                case 1:
                    mainForm.labelResult.Text = "Punctul este in interiorul acoperirii convexe";
                    break;
                default:
                    break;
            }
        }

        private void DrawPoints(Punct[] P, Punct M)
        {
            int nrPoints = P.Length;
            for (int i = 0; i < nrPoints; ++i)
                g.FillEllipse(brush, P[i].x - POINTDIM / 2, -1 * P[i].y - POINTDIM / 2, POINTDIM, POINTDIM);
            g.FillEllipse(brush2, M.x - POINTDIM / 2, -1 * M.y - POINTDIM / 2, POINTDIM, POINTDIM);
        }

        private void DrawCircles(Punct[] P)
        {
            int nrCircles = P.Length;
            for (int i = 0; i < nrCircles; ++i)
                g.DrawEllipse(pen, P[i].x - radius, -1 * P[i].y - radius, radius * 2, radius * 2);
        }

        private Punct[] DrawTangents(Punct[] P)
        {
            int nrCircles = P.Length;
            Stack<Punct> St = new Stack<Punct>();
            Punct T1, T2;
            for (int i = 0; i < nrCircles - 1; ++i)
            {
                T1 = FindTangentPoint(P[i], P[i + 1], 1);
                if (isRightTurn(P[i], P[i + 1], T1)) // trebuie vazut care din cele doua tangente externe este pe partea buna
                {
                    T1 = FindTangentPoint(P[i], P[i + 1], -1);
                    T2 = FindTangentPoint(P[i + 1], P[i], -1);
                }
                else
                    T2 = FindTangentPoint(P[i + 1], P[i], 1);
                g.DrawLine(pen, T1.x, -1 * T1.y, T2.x, -1 * T2.y);
                St.Push(T1);
                St.Push(T2);
            }
            if (nrCircles > 1)
            {
                T1 = FindTangentPoint(P[nrCircles - 1], P[0], 1);
                if (isRightTurn(P[nrCircles - 1], P[0], T1))
                {
                    T1 = FindTangentPoint(P[nrCircles - 1], P[0], -1);
                    T2 = FindTangentPoint(P[0], P[nrCircles - 1], -1);
                }
                else
                    T2 = FindTangentPoint(P[0], P[nrCircles - 1], 1);
                g.DrawLine(pen, T1.x, -1 * T1.y, T2.x, -1 * T2.y);
                St.Push(T1);
                St.Push(T2);
            }
            return St.ToArray();
        }

        private Punct FindTangentPoint(Punct A, Punct B, float sign)
        {
            // gaseste unul din cele doua puncte ce pot fi capatul de pe cercul C(A, RADIUS) al tangentei comune cu cercul C(B, RADIUS)
            // sign = -1 sau sign = 1 determina care din cele doua puncte se alege
            float x, y;
            Punct tangentPoint;
            if (Math.Abs(A.x - B.x) < EPS)
            {
                x = A.x - sign * radius;
                y = A.y;
            }
            else
            {
                float slope1 = (A.y - B.y) / (A.x - B.x);
                if (Math.Abs(slope1) < EPS)
                {
                    x = A.x;
                    y = A.y - sign * radius;
                }
                else
                {
                    float slope2 = (-1.0F) / slope1;
                    x = A.x + sign * radius * (1.0F / (float)Math.Sqrt(1.0F + slope2 * slope2));
                    y = A.y + sign * radius * (slope2 / (float)Math.Sqrt(1.0F + slope2 * slope2));
                }
            }
            tangentPoint = new Punct(x, y);
            return tangentPoint;
        }

        private bool isRightTurn(Punct A, Punct B, Punct C)
        {
            // determina daca ABC este viraj la dreapta
            float det = A.x * B.y + B.x * C.y + C.x * A.y - A.y * B.x - B.y * C.x - C.y * A.x;
            return (det < -EPS);
        }

        private bool isLeftTurn(Punct A, Punct B, Punct C)
        {
            // determina daca ABC este viraj la stanga
            float det = A.x * B.y + B.x * C.y + C.x * A.y - A.y * B.x - B.y * C.x - C.y * A.x;
            return (det > EPS);
        }

        private float Distance(Punct A, Punct B)
        {
            // returneaza distanta dintre punctele A si B
            return (float)Math.Sqrt((A.x - B.x) * (A.x - B.x) + (A.y - B.y) * (A.y - B.y));
        }

        private int InsideCircle(Punct C, Punct M)
        {
            float d = Distance(M, C);
            if (d - radius > EPS)
                return -1; // exterior
            if (d - radius < -EPS)
                return 1; // interior
            return 0; // pe cerc
        }
    }
}

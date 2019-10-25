using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;

using gfoidl.Imaging;

namespace DeteksiKerusakanSawah
{
    public delegate void MyDelegate(string input);

    delegate void SetTextCallback(string text);

    public delegate void DelegateThreadFinished();

    

    public partial class Form1 : Form
    {
        public System.Drawing.Bitmap sourceImage;
        public System.Drawing.Bitmap filteredImage;
        public System.Drawing.Bitmap originalImage;
        public System.Drawing.Bitmap GImage;

        private Image _originalImage;
        //private bool _selecting;
        //private Rectangle _selection;

        private BackgroundWorker backgroundWorker;
        public Stopwatch stopWatch;

        Boolean mouseClicked;
        Point startPoint = new Point();
        Point endPoint = new Point();
        Rectangle rectCropArea;
        string gbrfile;

        double dluassawah, dluasvariet, dhasilvar, dprediksi, dpersen, drugi, dbersih;

        public Form1()
        {
            InitializeComponent();

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);

            stopWatch = new Stopwatch();
            mouseClicked = false;
        }

        private void btbuka_Click(object sender, EventArgs e)
        {
            pictureBox3.Visible = false;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    button2.Enabled = true;
                    this.gbrfile = this.openFileDialog.FileName;
                    pictureBox1.Image = new Bitmap(openFileDialog.FileName);
                    originalImage = (Bitmap)pictureBox1.Image.Clone();
                    sourceImage = (Bitmap)pictureBox1.Image.Clone();
                    pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
                    _originalImage = pictureBox1.Image.Clone() as Image;
                
                }
                catch (NotSupportedException ex)
                {
                    MessageBox.Show("Format Image tidak Support: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show("Image Tidak Valid: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                    MessageBox.Show("Tidak Bisa Meload Image", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                btpotong.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //proses cluster
            button2.Enabled = false;
            button3.Enabled = true;

            stopWatch.Reset();
            stopWatch.Start();
            backgroundWorker.RunWorkerAsync();

            //proses perhitungan sawah
            //button1.Enabled = true;

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
            toolStripStatusLabel1.Text = e.UserState as String;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {


            if ((e.Cancelled == true))
            {
                toolStripStatusLabel1.Text = "Membatalkan!";
                button2.Enabled = true;
                button3.Enabled = false;
            }

            else if (!(e.Error == null))
            {
                toolStripStatusLabel1.Text = ("Error: " + e.Error.Message);
            }

            toolStripProgressBar1.Enabled = false;
            this.button2.Enabled = true;
            this.button3.Enabled = false;
        }



        // This method will run on a thread other than the UI thread.
        // Be sure not to manipulate any Windows Forms controls created
        // on the UI thread from this method.
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            backgroundWorker.ReportProgress(0, "Memulai...");

            filteredImage = (Bitmap)pictureBox1.Image.Clone();
            int numClusters = (int)numericUpDown2.Value;
            int maxIterations = (int)numericUpDown3.Value;
            double accuracy = (double)numericUpDown4.Value;



            List<ClusterPoint> points = new List<ClusterPoint>();


            for (int row = 0; row < originalImage.Width; ++row)
            {
                for (int col = 0; col < originalImage.Height; ++col)
                {

                    Color c2 = originalImage.GetPixel(row, col);
                    points.Add(new ClusterPoint(row, col, c2));

                }
            }



            List<ClusterCentroid> centroids = new List<ClusterCentroid>();

            //Create random points to use a the cluster centroids
            Random random = new Random();
            for (int i = 0; i < numClusters; i++)
            {
                int randomNumber1 = random.Next(sourceImage.Width);
                int randomNumber2 = random.Next(sourceImage.Height);
                centroids.Add(new ClusterCentroid(randomNumber1, randomNumber2, filteredImage.GetPixel(randomNumber1, randomNumber2)));
            }
            FCM alg = new FCM(points, centroids, 2, filteredImage, (int)numericUpDown2.Value);


            int k = 0;
            do
            {
                if ((backgroundWorker.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
                else
                {

                    k++;
                    alg.J = alg.CalculateObjectiveFunction();
                    alg.CalculateClusterCentroids();
                    alg.Step();
                    double Jnew = alg.CalculateObjectiveFunction();
                    Console.WriteLine("Run method i={0} accuracy = {1} delta={2}", k, alg.J, Math.Abs(alg.J - Jnew));
                    toolStripStatusLabel2.Text = "Precision " + Math.Abs(alg.J - Jnew);

                    // Format and display the TimeSpan value.
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", stopWatch.Elapsed.Hours, stopWatch.Elapsed.Minutes, stopWatch.Elapsed.Seconds, stopWatch.Elapsed.Milliseconds / 10);
                    toolStripStatusLabel3.Text = "Durasi: " + elapsedTime;

                    pictureBox2.Image = (Bitmap)alg.getProcessedImage;
                    backgroundWorker.ReportProgress((100 * k) / maxIterations, "Iterasi " + k);

                    if (Math.Abs(alg.J - Jnew) < accuracy) break;

                    
                }
            }
            while (maxIterations > k);
            Console.WriteLine("Done.");

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Save the segmented image
            pictureBox2.Image = (Bitmap)alg.getProcessedImage.Clone();
            alg.getProcessedImage.Save("segmented.png");

            // Create a new image for each cluster in order to extract the features from the original image
            double[,] Matrix = alg.U;
            Bitmap[] bmapArray = new Bitmap[centroids.Count];
            for (int i = 0; i < centroids.Count; i++)
            {
                bmapArray[i] = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppRgb);
            }

            for (int j = 0; j < points.Count; j++)
            {
                for (int i = 0; i < centroids.Count; i++)
                {
                    ClusterPoint p = points[j];
                    if (Matrix[j, i] == p.ClusterIndex)
                    {
                        bmapArray[i].SetPixel((int)p.X, (int)p.Y, p.OriginalPixelColor);
                    }
                }
            }

            // Save the image for each segmented cluster
            for (int i = 0; i < centroids.Count; i++)
            {
                bmapArray[i].Save("Cluster" + i + ".png");
            }


            // Resource cleanup...more work to do here to avoid memory problems!!!
            backgroundWorker.ReportProgress(100, "Done in " + k + " iterasi.");
            ////alg.Dispose();
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = null;
            }
            for (int i = 0; i < centroids.Count; i++)
            {
                centroids[i] = null;
            }
            alg = null;
            //centroids.Clear();
            //points.Clear();
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (backgroundWorker != null)
            {
                backgroundWorker.CancelAsync();
            }

            toolStripStatusLabel1.Text = "Membatalkan, mohon tunggu...";
        }

        private void btkeluar_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Anda Ingin Keluar dari aplikasi ini ?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        
        private void Form1_Load(object sender, EventArgs e)
        {
            btpotong.Enabled = false;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseClicked = true;

            startPoint.X = e.X;
            startPoint.Y = e.Y;
            //Menampilkan Koordinat
            X1.Text = startPoint.X.ToString();
            Y1.Text = startPoint.Y.ToString();

            endPoint.X = -1;
            endPoint.Y = -1;

            rectCropArea = new Rectangle(new Point(e.X, e.Y), new Size());
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = true;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Point ptCurrent = new Point(e.X, e.Y);

            if (mouseClicked)
            {
                if (endPoint.X != -1)
                {
                    // Menampilkan Koordinat
                    X1.Text = startPoint.X.ToString();
                    Y1.Text = startPoint.Y.ToString();
                    X2.Text = e.X.ToString();
                    Y2.Text = e.Y.ToString();
                }

                endPoint = ptCurrent;

                if (e.X > startPoint.X && e.Y > startPoint.Y)
                {
                    rectCropArea.Width = e.X - startPoint.X;
                    rectCropArea.Height = e.Y - startPoint.Y;
                }
                else if (e.X < startPoint.X && e.Y > startPoint.Y)
                {
                    rectCropArea.Width = startPoint.X - e.X;
                    rectCropArea.Height = e.Y - startPoint.Y;
                    rectCropArea.X = e.X;
                    rectCropArea.Y = startPoint.Y;
                }
                else if (e.X > startPoint.X && e.Y < startPoint.Y)
                {
                    rectCropArea.Width = e.X - startPoint.X;
                    rectCropArea.Height = startPoint.Y - e.Y;
                    rectCropArea.X = startPoint.X;
                    rectCropArea.Y = e.Y;
                }
                else
                {
                    rectCropArea.Width = startPoint.X - e.X;
                    rectCropArea.Height = startPoint.Y - e.Y;
                    rectCropArea.X = e.X;
                    rectCropArea.Y = e.Y;
                }
                pictureBox1.Refresh();
            }
        }

       

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            mouseClicked = false;

            if (endPoint.X != -1)
            {
                Point currentPoint = new Point(e.X, e.Y);
                // Menampilkan Koordinat
                X2.Text = e.X.ToString();
                Y2.Text = e.Y.ToString();

            }
            endPoint.X = -1;
            endPoint.Y = -1;
            startPoint.X = -1;
            startPoint.Y = -1;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Pen drawLine = new Pen(Color.Yellow);
            drawLine.DashStyle = DashStyle.Dash;
            e.Graphics.DrawRectangle(drawLine, rectCropArea);
        }

        private void btpotong_Click(object sender, EventArgs e)
        {
            button2.Enabled = true;
            pictureBox3.Visible = true;
            pictureBox3.Refresh();

            //var gambar = new Bitmap(this.gbrfile);
            //var graph1 = Graphics.FromImage(gambar);

            Bitmap sourceBitmap = new Bitmap(pictureBox1.Image, pictureBox1.Width, pictureBox1.Height);
            Graphics g = pictureBox3.CreateGraphics();
            //Graphics g = Graphics.FromImage(sourceBitmap);

            if (!chkCropKordinat.Checked)
            {
                g.DrawImage(sourceBitmap, new Rectangle(0, 0, pictureBox3.Width, pictureBox3.Height), rectCropArea, GraphicsUnit.Pixel);
                sourceBitmap.Dispose();
            }
            else
            {

                int x1, x2, y1, y2;
                Int32.TryParse(CX1.Text, out x1);
                Int32.TryParse(CX2.Text, out x2);
                Int32.TryParse(CY1.Text, out y1);
                Int32.TryParse(CY2.Text, out y2);

                if ((x1 < x2 && y1 < y2))
                {
                    rectCropArea = new Rectangle(x1, y1, x2 - x1, y2 - y1);
                }
                else if (x2 < x1 && y2 > y1)
                {
                    rectCropArea = new Rectangle(x2, y1, x1 - x2, y2 - y1);
                }
                else if (x2 > x1 && y2 < y1)
                {
                    rectCropArea = new Rectangle(x1, y2, x2 - x1, y1 - y2);
                }
                else
                {
                    rectCropArea = new Rectangle(x2, y2, x1 - x2, y1 - y2);
                }

                pictureBox1.Refresh(); // Mereposisi kotak putus-putus ke lokasi baru sesuai koordinat yang dimasukkan.

                g.DrawImage(sourceBitmap, new Rectangle(0, 0, pictureBox3.Width, pictureBox3.Height), rectCropArea, GraphicsUnit.Pixel);

                //pictureBox3.Image = sourceBitmap;
                //pictureBox2.Image = pictureBox3.Image;
                
                sourceBitmap.Dispose();

            }

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GImage = (Bitmap)pictureBox2.Image.Clone();
            //Color Dominan
            int r = 0;
            int g = 0;
            int b = 0;
            int luas = 0; 
            int sisa = 0;

            int total = 0;

            for (int x = 0; x < GImage.Width; x++)
            {
                for (int y = 0; y < GImage.Height; y++)
                {
                    Color clr = GImage.GetPixel(x, y);

                    r += clr.R;
                    g += clr.G;
                    b += clr.B;

                    total++;
                }
            }

            //Calculate average
            r /= total;
            g /= total;
            b /= total;

            
            if (g > 100)
            {
                luas = g - 100;
                sisa = 100 - luas;
            } else
            {
                luas = g;
                sisa = 100 - luas;
            }

            textBox3.Text = Convert.ToString(luas);
            textBox4.Text = Convert.ToString(sisa);

            dluassawah = Convert.ToDouble(txtluas.Text);
            dluasvariet = Convert.ToDouble(textBox2.Text);
            dhasilvar = Convert.ToDouble(textBox1.Text);
            dpersen = Convert.ToDouble(textBox4.Text);

            dprediksi = (dluassawah / dluasvariet) * dhasilvar;
            drugi = (dpersen / 100) * dprediksi;
            dbersih = dprediksi - drugi;

            LKerusakan.Text = "Persentase Kerusakan : " + textBox4.Text + "%";
            LKerugian.Text = "Kerugian :" + Convert.ToString(drugi);
            LBersih.Text = "Hasil Bersih : " + Convert.ToString(dbersih);
        }

        private void cbjenis_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cbjenis.SelectedIndex == -1)
            {
                txthasilvar.Text = string.Empty;
            } else {
                txthasilvar.Text = cbjenis.SelectedItem.ToString();
                if (txthasilvar.Text == "Ciherang")
                {
                    textBox1.Text = "8000";
                }
                if (txthasilvar.Text == "Situbagedit")
                {
                    textBox1.Text = "5000";
                }
                if (txthasilvar.Text == "IR 64")
                {
                    textBox1.Text = "6000";
                }
            }

        }
    }
}

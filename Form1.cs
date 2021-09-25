using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Net;

namespace PhotoSmort
{
    public partial class Form1 : Form
    {
        private List<Image> list = new List<Image>();
        private int selected_index;
        private int slide_index;
        private int max_slide_index;
        public Form1()
        {
            InitializeComponent();

            selected_index = -1;
            slide_index = 0;
            var slider = new ImageItemSlider { Parent = this, Top = 100, Width = ClientSize.Width, Height = 200, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left };
            slider.Build(list);

            Button P_prev = new Button() { Parent = this, Text = "Prev", BackColor = Color.White, Top = 50, Name = "B_Prev" };

            Prev_button.Enabled = false;
            Next_button.Enabled = false;

            Prev_button.Click += delegate 
            { 
                slider.CurentSlide--; slide_index--; if (slide_index == 0) { Prev_button.Enabled = false; if (max_slide_index > 0) Next_button.Enabled = true; else Next_button.Enabled = false; }
            };
            Next_button.Click += delegate { slider.CurentSlide++; slide_index++; Prev_button.Enabled = true; if (slide_index == max_slide_index) Next_button.Enabled = false; };

            this.tableLayoutPanel3.Controls.Add(slider, 1, 0);

            
            //var pb = new PictureBox { Parent = this, Top = 400, Left = Width / 2 - 200, Width = 400, Height = 400, SizeMode = PictureBoxSizeMode.Zoom };
            //this.tableLayoutPanel4.Controls.Add(pb, 1, 0);
            //pb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.ColumnStyles[1].Width = tableLayoutPanel4.Height;

            slider.ImageClick += delegate 
            {
                selected_index = slider.HoveredImageIndex;
                pictureBox1.Image = list[slider.HoveredImageIndex]; 
                if (pictureBox1.Image != null)
                {
                    float s = float.Parse(Convert.ToString(Convert.ToDouble(pictureBox1.Image.Width) / Convert.ToDouble(pictureBox1.Image.Height)));
                    this.tableLayoutPanel4.ColumnStyles[1].Width = tableLayoutPanel4.Height * s;
                }
            };

        }

        public class ImageItemSlider : Control
        {
            public int ImagesPerSlide { get; set; }
            public int CurentSlide { get; set; }
            public Padding ImagePaddings { get; set; }
            private ImageItemDrawer ImageItemDrawer { get; set; }
            public int AnimateSpeed { get; set; }
            public int HoveredImageIndex { get; set; }
            public event EventHandler ImageClick = delegate { };
            public bool IsAnimatedNow { get; private set; }

            private List<Image> items;
            private int HScroll;

            public ImageItemSlider()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

                ImagesPerSlide = 5;
                ImageItemDrawer = new ImageItemDrawer();
                AnimateSpeed = 30;
                Cursor = Cursors.Hand;

                Application.Idle += Animate;
            }

            private void Animate(object sender, EventArgs e)
            {
                var expectedHScroll = ClientSize.Width * CurentSlide;
                if (HScroll != expectedHScroll)
                {
                    //var speed = Math.Abs(HScroll - expectedHScroll) <= AnimateSpeed ? 1 : AnimateSpeed;
                    //HScroll += speed * Math.Sign(expectedHScroll - HScroll);
                    HScroll += (expectedHScroll - HScroll);
                    Invalidate();
                    IsAnimatedNow = true;
                }
                else
                    IsAnimatedNow = false;
            }

            public void Build(List<Image> items)
            {
                this.items = items;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var imgWidth = ClientSize.Width / ImagesPerSlide;

                for (int i = 0; i < items.Count; i++)
                {
                    var x = i * imgWidth - HScroll;
                    var rect = new Rectangle(x, ClientRectangle.Top, imgWidth, ClientSize.Height);
                    if (e.ClipRectangle.IntersectsWith(rect))
                    {
                        if (i == HoveredImageIndex)
                            using (var brush = new LinearGradientBrush(rect, BackColor, Color.Orange, 90))
                            {
                                e.Graphics.FillRectangle(brush, rect);
                            }

                        ImageItemDrawer.Draw(e.Graphics, rect, items[i]);
                    }
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (!IsAnimatedNow)
                {
                    HoveredImageIndex = PointToIndex(e.Location);
                    Invalidate();
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);

                if (!IsAnimatedNow)
                    if (e.Button == MouseButtons.Left)
                        if (HoveredImageIndex >= 0 && HoveredImageIndex < items.Count)
                            ImageClick(this, EventArgs.Empty);
            }

            /// <summary>
            /// Returns index of ImageIndex in given point
            /// </summary>
            public int PointToIndex(Point p)
            {
                var imgWidth = ClientSize.Width / ImagesPerSlide;
                return (p.X + HScroll) / imgWidth;
            }
        }

        public class ImageItemDrawer
        {
            public int BottomStripHeight = 30;
            public Color BottomStripColor = Color.FromArgb(100, 0, 0, 0);
            public Color BottomStripTextColor = Color.White;
            public Font BottomStripTextFont = new Font(FontFamily.GenericSansSerif, 12);
            public Padding Paddings = new Padding(5, 2, 5, 2);

            public virtual void Draw(Graphics gr, Rectangle rect, Image item)
            {
                //image rect with paddings
                rect = new Rectangle(rect.Left + Paddings.Left, rect.Top + Paddings.Top, rect.Width - Paddings.Left - Paddings.Right, rect.Height - Paddings.Top - Paddings.Bottom);
                //bottom strip rect
                var strip = new Rectangle(rect.Left, rect.Top + rect.Height - BottomStripHeight, rect.Width, BottomStripHeight);
                //draw image
                gr.DrawImage(item, rect);
                //draw bottom strip
                using (var brush = new SolidBrush(BottomStripColor))
                {
                    gr.FillRectangle(brush, strip);
                    brush.Color = BottomStripTextColor;
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center };
                    //gr.DrawString(item.Comment, BottomStripTextFont, brush, strip, sf);
                    sf.Alignment = StringAlignment.Far;
                    //gr.DrawString(item.Duration.ToString(), BottomStripTextFont, brush, strip, sf);
                }
            }
        }



        //public class ImageItem
        //{
        //    public Image Image { get; set; }
        //}

        private void tableLayoutPanel4_SizeChanged(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                float s = float.Parse(Convert.ToString(Convert.ToDouble(pictureBox1.Image.Width) / Convert.ToDouble(pictureBox1.Image.Height)));
                this.tableLayoutPanel4.ColumnStyles[1].Width = tableLayoutPanel4.Height*s;
            }
        }

        private void открытьФотоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                list.Add(Image.FromFile(openFileDialog1.FileName));

                //if (list.Count > 5) Next_button.Enabled = true;
                //else Next_button.Enabled = false;

                int t = list.Count;

                for (;;)
                {
                    if (list.Count < 5) break;

                    if (t % 5 == 0)
                    {
                        max_slide_index = t / 5;
                        break;
                    }
                    else t--;
                }

                if (selected_index != max_slide_index) Next_button.Enabled = true;
            }
        }

        private void закрытьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            list.Clear();
            pictureBox1.Image = null;
            if (list.Count > 5) Next_button.Enabled = true;
            else Next_button.Enabled = false;

            int t = list.Count;

            for (; ; )
            {
                if (list.Count < 5) break;

                if (t % 5 == 0)
                {
                    max_slide_index = t / 5;
                    break;
                }
                else t--;
            }

            if (selected_index < max_slide_index) Next_button.Enabled = true;
            if (selected_index > max_slide_index)
            {
                Prev_button.Pre
            }
        }

        private void закрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selected_index != -1)
            {
                list.Remove(list[selected_index]);
                if (list.Count != 0)
                {
                    pictureBox1.Image = list[0];
                }
                else pictureBox1.Image = null;

                if (list.Count > 5) Next_button.Enabled = true;
                else Next_button.Enabled = false;
            }
        }
    }
}

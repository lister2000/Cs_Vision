using System;
using System.Collections.Generic;
using System.Linq;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Text;
//using System.Threading.Tasks;
//using OpenCvSharp.Extensions;
using System.Windows.Forms;
using OpenCvSharp;
using System.Threading;
using OpenCvSharp.Dnn;
using System.Text.RegularExpressions;

namespace Cs_Vision
{
    public partial class MainWindow : Form
    {
        int  text_id;
        bool on_camera = false;
        bool is_inited = false;
        Form2 fm2 = new Form2();
        Form3 fm3 = new Form3();
        CsPram csp = CsPram.Instance;
        CsTools cst = CsTools.Instance;
        private TimerComponent timerComponent;
        private Thread timerThread;
        List <TextBox> list_textboxes = new List <TextBox>(); 
        public MainWindow()
        {
            InitializeComponent();
            timerComponent = new TimerComponent();
            timerThread = new Thread(new ThreadStart(timerComponent.Start));
            list_textboxes.Add(data_textBox1); list_textboxes.Add(data_textBox2); list_textboxes.Add(data_textBox3);
            list_textboxes.Add(data_textBox4); list_textboxes.Add(data_textBox5); list_textboxes.Add(data_textBox6);
            list_textboxes.Add(data_textBox7); list_textboxes.Add(data_textBox8); list_textboxes.Add(data_textBox9);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            csp.frame_image = Cv2.ImRead("test.jpg");
            this.Width = pictureBox1.Width + panel_leftside.Width + 2;
            csp.bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(csp.frame_image);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBox1.Image = csp.bitmap;

            Cv2.CvtColor(csp.frame_image, csp.hsv_image, ColorConversionCodes.BGR2HSV);
            IniteUI();
            is_inited = true;
            timerThread.Start();

        }

        private void MainWindow_SizeChanged(object sender, EventArgs e)
        {
            if (!is_inited)
            {
                return;
            }
            IniteUI();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender as PictureBox == pictureBox1)
            {
                csp.is_mouseclick = true;
                csp.is_moveregion = true;
                csp.is_drawregion = true;
                int cv_x = (int)(e.X / (float)pictureBox1.Width * csp.frame_image.Width);
                int cv_y = (int)(e.Y / (float)pictureBox1.Height * csp.frame_image.Height);
                csp.draw_rect = new Rect(cv_x, cv_y, 0, 0);
                csp.pick_point = new OpenCvSharp.Point(cv_x, cv_y);
                csp.last_point = new OpenCvSharp.Point(cv_x, cv_y);
                csp.curr_point = new OpenCvSharp.Point(cv_x, cv_y);



                if (e.Button == MouseButtons.Middle && csp.hsv_image != null)
                {
                    if (cst.InRectangle(new OpenCvSharp.Point(cv_x, cv_y), csp.pickregion.region))
                    {
                        for (int i = 0; i < csp.some_rects.Count; i++)
                        {
                            if (csp.some_rects[i].index == csp.pickregion.index)
                            {
                                unsafe
                                {
                                    byte* pick_color = (byte*)csp.hsv_image.Ptr(cv_y, cv_x);
                                    csp.some_rects[i].color = new Vec3b(pick_color[0], pick_color[1], pick_color[2]);
                                    fm2.trackBar2.Value = (int)(pick_color[0] / 255.0f * 100);
                                    fm2.trackBar3.Value = (int)(pick_color[1] / 255.0f * 100);
                                    fm2.trackBar4.Value = (int)(pick_color[2] / 255.0f * 100);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender as PictureBox == pictureBox1)
            {
                if (csp.is_drawregion)
                {
                    int cv_x = (int)(e.X / (float)pictureBox1.Width * csp.frame_image.Width);
                    int cv_y = (int)(e.Y / (float)pictureBox1.Height * csp.frame_image.Height);
                    cv_x = cv_x < 0 ? 0 : cv_x;
                    cv_x = cv_x < csp.frame_image.Cols ? cv_x : csp.frame_image.Cols;
                    cv_y = cv_y < 0 ? 0 : cv_y;
                    cv_y = cv_y < csp.frame_image.Rows ? cv_y : csp.frame_image.Rows;
                    csp.curr_point = new OpenCvSharp.Point(cv_x, cv_y);
                    csp.draw_rect.X = Math.Min(cv_x, csp.pick_point.X);
                    csp.draw_rect.Y = Math.Min(cv_y, csp.pick_point.Y);
                    csp.draw_rect.Width = Math.Abs(cv_x - csp.pick_point.X);
                    csp.draw_rect.Height = Math.Abs(cv_y - csp.pick_point.Y);
                    if (!csp.is_zoomregion)
                    {
                        if (cst.InRectangle(csp.curr_point, csp.pickregion.region)
                        && Math.Abs(csp.curr_point.X - csp.pickregion.region.X - csp.pickregion.region.Width / 2) < csp.pickregion.region.Width / 4
                        && Math.Abs(csp.curr_point.Y - csp.pickregion.region.Y - csp.pickregion.region.Height / 2) < csp.pickregion.region.Height / 4)
                        {
                            csp.draw_rect.Width = csp.draw_rect.Height = 0;
                        }
                        else
                        {
                            csp.is_moveregion = false;
                        }
                        if (csp.draw_rect.Width * csp.draw_rect.Height > csp.AREA3600)
                        {
                            csp.draw_rect = cst.Urect(csp.draw_rect, new Rect(0, 0, csp.frame_image.Cols, csp.frame_image.Rows));
                            csp.pickregion.region = csp.draw_rect;
                            csp.pickregion.type = 2;
                        }
                    }
                }

                if (e.Button == MouseButtons.Middle)
                {
                    csp.is_zoomregion = true;
                    csp.zoompos = csp.curr_point;
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            csp.is_textupdate = true;
            if (sender as PictureBox == pictureBox1)
            {
                csp.is_mouseclick = false;
                csp.is_drawregion = false;
                csp.is_moveregion = false;
                //处理中键事件

                if (e.Button == MouseButtons.Right)
                {
                    if (cst.InRectangle(csp.curr_point, csp.pickregion.region)&&
                        Math.Abs(csp.curr_point.X - csp.pickregion.region.X - csp.pickregion.region.Width / 2) < csp.pickregion.region.Width / 4 &&
                        Math.Abs(csp.curr_point.Y - csp.pickregion.region.Y - csp.pickregion.region.Height / 2) < csp.pickregion.region.Height / 4)
                    {
                        csp.is_dellregion = true;
                    }
                    else
                    {
                        csp.dict_cpoly.Clear();
                        for (int i = 0; i < csp.some_rects.Count; i++)
                        {
                            csp.some_rects[i].csdata.markdatas = new float[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        }
                    }
                }
                //添加处理区域
                if (csp.draw_rect.Width * csp.draw_rect.Height > csp.AREA3600 && !csp.is_zoomregion)
                {
                    csp.pickregion.index = csp.some_rects.Count;
                    if (csp.is_trackeron)
                    {
                        CsRegion csrt = new CsRegion(csp.draw_rect, new Vec3b(), 50, 1, csp.pickregion.index);
                        csrt.tracker.TrackOn(csp.frame_image, csp.pickregion.region);
                        csp.some_rects.Add(csrt);
                        csp.pickregion.type = 1;
                    }
                    else
                    {
                        csp.some_rects.Add(new CsRegion(csp.draw_rect, new Vec3b(), 50, 2, csp.pickregion.index));
                        csp.pickregion.type = 2;
                    }
                }
            }
            csp.is_zoomregion = false;
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = false;
            DialogResult result = MessageBox.Show("确定退出", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes) e.Cancel = false; 
            else  e.Cancel = true; 
        }

        private void 重置参数ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //需考虑跨线程数据同步代码移动到程序块RunOpencv()内部；
            csp.run_reset = true;
        }

        private void 设置跟踪ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            csp.is_trackeron = true;
            csp.is_trackerrun = false;
            //使用DlibDotNet实现目标追踪//https://zhuanlan.zhihu.com/p/141926844
        }

        private void 启动跟踪ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            csp.is_trackerrun = true;
            csp.is_trackeron = false;
            //csp.is_dellregion = true;
        }

        private void 调试工具ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fm2.Visible == false)
            {
                fm2 = new Form2();
                fm2.Show();
            }
        }

        private void 标定工具ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fm3.Visible == false)
            {
                fm3 = new Form3();
                fm3.Show();
            }
        }

        ColorDetector cdetect = new ColorDetector();
        private void RunOpencv()
        {
            //while (on_opencv)
            {
                if (csp.run_reset)
                {
                    //跨线程数据同步
                    csp.run_reset = csp.is_trackeron = csp.is_trackerrun = false;
                    csp.draw_rect = csp.pickregion.region = new Rect();
                    csp.some_rects.Clear();csp.dict_cpoly.Clear();
                }
                if (csp.is_loaddatas)
                {
                    csp.is_loaddatas = csp.is_trackeron = csp.is_trackerrun = false;
                    csp.draw_rect = csp.pickregion.region = new Rect();
                    csp.some_rects.Clear();csp.dict_cpoly.Clear();
                    csp.some_rects = PolyClass.LoadSomeRects("rect_datas");
                    csp.dict_cpoly = PolyClass.LoadDictPoly("dict_datas");
                    csp.pickregion = PolyClass.pick_irect;
                    for (int i = 0; i < csp.some_rects.Count; i++)
                    {
                        if (csp.some_rects[i].type == 1)
                        {
                            csp.some_rects[i].tracker = new AsDll.AsDlib();
                            csp.some_rects[i].tracker.TrackOn(csp.frame_image, csp.some_rects[i].rect);
                            csp.is_trackerrun = true;
                        }
                    }
                }
                if (csp.run_loop)
                {
                    if (on_camera && !csp.is_trackeron)
                    {
                        csp.videocap.Read(csp.frame_image);
                    }
                    csp.izoomVal = csp.frame_image.Cols / 320;

                    Cv2.CopyTo(csp.frame_image, csp.result_image);

                    if (csp.is_zoomregion || on_camera)
                    {
                        int H = csp.frame_image.Rows;
                        int W = csp.frame_image.Cols;

                        int zH = (int)(H / csp.zoomraito);
                        int zW = (int)(W / csp.zoomraito);
                        int newX = csp.zoompos.X - zW / 2;
                        int newY = csp.zoompos.Y - zH / 2;

                       if (newX < 0) newX = 0;
                       if (newY < 0) newY = 0;
                       if (newX + W / csp.zoomraito > W) newX = W - zW;
                       if (newY + H / csp.zoomraito > H) newY = H - zH;

                        csp.zoomrect = new Rect(newX, newY, zW, zH);
                        csp.zoomimage = csp.result_image.Clone(csp.zoomrect);
                        Cv2.Resize(csp.zoomimage, csp.zoomimage, new OpenCvSharp.Size(W, H));
                    }
                    if (csp.zoomimage.Size() == csp.result_image.Size())
                    {
                        csp.result_image = csp.zoomimage.Clone();
                    }
                    Cv2.CvtColor(csp.result_image, csp.hsv_image, ColorConversionCodes.BGR2HSV);


                    if (csp.pickregion.region.Width * csp.pickregion.region.Height > csp.AREA3600||csp.some_rects.Count>0)
                    {
                        for (int i = 0; i < csp.some_rects.Count; i++)
                        {
                            Mat itemp  = new Mat(csp.result_image, csp.some_rects[i].rect);
                            Mat icolor = new Mat(csp.result_image, csp.some_rects[i].rect);
                            Mat imask  = new Mat(csp.some_rects[i].rect.Size, MatType.CV_8UC1);
                            if (csp.some_rects[i].color != new Vec3b())
                            {
                                cdetect.setTargetColor(csp.some_rects[i].color);
                                cdetect.setColorDistanceThreshold((int)(csp.thrldVal * 2.55f));
                                imask = cdetect.process(new Mat(csp.hsv_image, csp.some_rects[i].rect));
                            }
                            else
                            {
                                Cv2.CvtColor(itemp, imask, ColorConversionCodes.BGR2GRAY, 0);
                                Cv2.Threshold(imask, imask, csp.some_rects[i].value, 255, ThresholdTypes.Binary);
                            }
                            if (csp.pickregion.index == i) pictureBox2.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(imask);

                            cst.TestMeasure(icolor, imask, csp.some_rects[i].index);

                            if (csp.is_drawregion && cst.InRectangle(csp.curr_point, csp.some_rects[i].rect))
                            {
                                csp.pickregion.region = csp.some_rects[i].rect;
                                csp.pickregion.index  = csp.some_rects[i].index;
                                csp.pickregion.index  = i;//切换选区;
                            }
                            if (csp.some_rects[i].type!=1)
                            {
                                Cv2.Rectangle(csp.result_image, new Rect(csp.some_rects[i].rect.X - 1, csp.some_rects[i].rect.Y - 1,
                                    csp.some_rects[i].rect.Width + 2, csp.some_rects[i].rect.Height + 2), csp.some_rects[i].ok_ng, csp.izoomVal);
                            }
                            Cv2.PutText(csp.result_image, "id:" + (csp.some_rects[i].index), csp.some_rects[i].rect.TopLeft,
                                HersheyFonts.HersheyPlain, csp.izoomVal, csp.gray_100, csp.izoomVal);
                        }

                        cst.oneCross(csp.result_image, new OpenCvSharp.Point(csp.pickregion.region.X + csp.pickregion.region.Width / 2, csp.pickregion.region.Y + csp.pickregion.region.Height / 2), csp.cyan_color, csp.izoomVal);

                        if (!csp.is_trackeron)
                        {
                            if (csp.pickregion.type!=1)
                                Cv2.Rectangle(csp.result_image, new OpenCvSharp.Rect(csp.pickregion.region.X - 1, csp.pickregion.region.Y - 1, csp.pickregion.region.Width + 2, csp.pickregion.region.Height + 2), csp.pickregion.ok_ng, csp.izoomVal);
                            if (!csp.is_drawregion)
                            {
                                Cv2.PutText(csp.result_image, "id:" + (csp.pickregion.index), csp.pickregion.region.TopLeft,
                                HersheyFonts.HersheyPlain, csp.izoomVal, csp.cyan_color, csp.izoomVal);
                            }
                        }
                        //删除选择
                        if (csp.is_dellregion)
                        {
                            csp.is_textupdate = true;
                            csp.is_dellregion = false;
                            if (csp.some_rects.Count > 0)
                            {
                                for (int i = csp.some_rects.Count - 1; i >= 0; i--)
                                {
                                    if (csp.some_rects[i].index == csp.pickregion.index)
                                    {
                                        csp.some_rects.RemoveAt(i);
                                        for (int j = csp.dict_cpoly.Count - 1; j >= 0; j--)
                                        {
                                            if (csp.dict_cpoly[j].index == csp.pickregion.index) csp.dict_cpoly.RemoveAt(j);
                                        }
                                        break;
                                    }
                                }
                                for (int i = csp.some_rects.Count - 1; i >= 0; i--)
                                {
                                    csp.pickregion.region = csp.some_rects[i].rect;
                                    csp.some_rects[i].index = csp.pickregion.index = i;
                                    csp.pickregion.type = csp.some_rects[i].type;
                                    for (int j = csp.dict_cpoly.Count - 1; j >= 0; j--)
                                    { 
                                        csp.dict_cpoly[j].index = i;//重新分配
                                    }
                                }
                            }
                            if (csp.some_rects.Count == 0)
                            {
                                csp.dict_cpoly.Clear();
                                csp.pickregion.region = new Rect();
                                csp.pickregion.type = -1;
                            }
                        }
                    }
                    //移动矩形
                    if (csp.is_moveregion)
                    {
                        for (int i = 0; i < csp.some_rects.Count; i++)
                        {
                            if (csp.some_rects[i].rect.Size == csp.pickregion.region.Size && cst.InRectangle(csp.curr_point, csp.pickregion.region))
                            {
                                csp.pickregion.region.X += (csp.curr_point.X - csp.last_point.X);
                                csp.pickregion.region.Y += (csp.curr_point.Y - csp.last_point.Y);

                                csp.pickregion.region = cst.Urect(csp.pickregion.region, new Rect(0, 0, csp.frame_image.Cols, csp.frame_image.Rows));
                                csp.some_rects[i].rect = csp.pickregion.region;
                            }
                        }
                    }
                    if (csp.is_trackeron)
                    {
                        //跟踪区域
                        Cv2.Rectangle(csp.result_image, csp.draw_rect, csp.blue_color, csp.izoomVal);
                        if (csp.some_rects.Count > 0)
                        {
                            for (int i = 0; i < csp.some_rects.Count; i++)
                            {
                                if (csp.some_rects[i].type == 1)
                                {
                                    Rect trect = csp.some_rects[i].rect;
                                    Cv2.Rectangle(csp.result_image, new OpenCvSharp.Rect(trect.X - 1, trect.Y - 1, trect.Width + 2, trect.Height + 2), csp.blue_color, csp.izoomVal);
                                }
                            }
                        }
                    }
                    if (csp.is_trackerrun)
                    {
                        //更新跟踪
                        for (int i = 0; i < csp.some_rects.Count; i++)
                        {
                            if (csp.some_rects[i].type == 1)
                            {
                                Rect trect = csp.some_rects[i].tracker.TrackUpdate(csp.frame_image);
                                Cv2.Rectangle(csp.result_image, new Rect(trect.Location, csp.some_rects[i].rect.Size), csp.blue_color, csp.izoomVal);
                                csp.some_rects[i].rect.Location= trect.Location;
                                if (csp.pickregion.index == i) csp.pickregion.region.Location = trect.Location;
                            }
                        }
                    }

                    csp.bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(csp.result_image);
                    if (csp.bitmap != null) pictureBox1.Image = csp.bitmap;
                    csp.last_point = csp.curr_point;
                    if (csp.run_mode == 1)
                    {
                        csp.run_loop = false;
                    }
                    if (csp.run_mode == 2)
                    {
                        csp.run_loop = true;
                    }
                    //Cv2.WaitKey(30);
                }
            }
        }
    
        private void IniteUI()
        {
            panel_upside.Width = this.Width;
            pictureBox1.Width = this.Width - panel_leftside.Width;
            pictureBox1.Height = this.Height - panel_upside.Height;
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            panel_leftside.Width = this.Size.Width - pictureBox1.Width;
            panel_leftside.Height = pictureBox1.Height;
            panel_leftside.Location = new System.Drawing.Point(pictureBox1.Location.X + pictureBox1.Width + 2, pictureBox1.Location.Y);

            pictureBox2.Width = panel_leftside.Width;
            pictureBox2.Height = (int)(pictureBox1.Height * 0.5f) - 30;
            pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;

            panel_param1.Width = pictureBox2.Width;
            panel_param1.Height = (int)(pictureBox1.Height * 0.5f) + 30;
            panel_param1.Location = new System.Drawing.Point(pictureBox2.Location.X, pictureBox2.Location.Y + pictureBox2.Height);
        }

        private void UpdateUI(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateUI), text);
            }
            else
            {
                //更新滑块数值
                if (csp.is_textupdate)
                {
                    csp.is_textupdate = false;
                    if (csp.some_rects.Count > 0)
                    {
                        fm2.label_thldval.Text = csp.some_rects[csp.pickregion.index].value.ToString();
                        fm2.trackBar1.Value = csp.some_rects[csp.pickregion.index].value;
                        fm2.Update();
                    }
                }
                //更新数据集合
                for (int n = 0; n < csp.some_rects.Count; n++)
                {
                    if (csp.some_rects[n].index == csp.pickregion.index)
                    {
                        csp.current_csdata = csp.some_rects[n].csdata;//vvv3
                        data_textBox1.Text = csp.current_csdata.markdatas[0].ToString();
                        data_textBox2.Text = csp.current_csdata.markdatas[1].ToString();
                        data_textBox3.Text = csp.current_csdata.markdatas[2].ToString();
                        data_textBox4.Text = csp.current_csdata.markdatas[3].ToString();
                        data_textBox5.Text = csp.current_csdata.markdatas[4].ToString();
                        data_textBox6.Text = csp.current_csdata.markdatas[5].ToString();
                        data_textBox7.Text = csp.current_csdata.markdatas[6].ToString();
                        data_textBox8.Text = csp.current_csdata.markdatas[7].ToString();
                        data_textBox9.Text = csp.current_csdata.markdatas[8].ToString();

                        for (int i = 0; i < 9; i++)
                        {
                            if (csp.current_csdata.markdatas[i] > csp.some_rects[csp.pickregion.index].maxdatas[i] ||
                                csp.current_csdata.markdatas[i] < csp.some_rects[csp.pickregion.index].mindatas[i])
                            {
                                list_textboxes[i].ForeColor = Color.Red;
                            }
                            else
                            {
                                list_textboxes[i].ForeColor = Color.Black;
                            }
                        }
                    }

                    for (int i = 0; i < 9; i++)
                    {
                        if (csp.some_rects[n].csdata.markdatas[i] <= csp.some_rects[n].maxdatas[i] &&
                            csp.some_rects[n].csdata.markdatas[i] >= csp.some_rects[n].mindatas[i])
                        {
                            csp.some_rects[n].ok_ng = csp.gray_128;
                            if (csp.some_rects[n].index == csp.pickregion.index)
                            {
                                csp.pickregion.ok_ng = csp.cyan_color;
                            }
                        }
                        else
                        {
                            csp.some_rects[n].ok_ng = csp.red_color;
                            if (csp.some_rects[n].index == csp.pickregion.index)
                            {
                                csp.pickregion.ok_ng=csp.red_color;
                            }
                            break;
                        }
                    }
                }
            }
        }

        private class TimerComponent
        {
            private bool isRunning;
            private DateTime startTime;
            public void Start()
            {
                isRunning = true;
                startTime = DateTime.Now;
                while (isRunning)
                {
                    TimeSpan elapsed = DateTime.Now - startTime;
                    string text = string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
                    MainWindow? form = Application.OpenForms.OfType<MainWindow>().FirstOrDefault();
                    if (form != null)
                    {
                        form.UpdateUI(text);
                        form.RunOpencv();
                    }
                    Thread.Sleep(50);
                }
            }
            public void Stop()
            {
                isRunning = false;
            }
        }
        private void 相机开启ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            on_camera = true;
            重置参数ToolStripMenuItem_Click(sender, e);
        }
        private void 相机关闭ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            on_camera = false;
            csp.frame_image = Cv2.ImRead("test.jpg");
            重置参数ToolStripMenuItem_Click(sender, e);
        }
        private void 单次运行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            csp.run_loop = true;
            csp.run_mode = 1;
        }
        private void 多次运行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            csp.run_loop = true;
            csp.run_mode = 2;
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D)
            {
                csp.is_dellregion = true;
                csp.dict_cpoly.Clear();
            }
            if (e.KeyCode == Keys.ShiftKey)
            {
                csp.some_rects.Clear();
                csp.pickregion.region = new Rect();
            }
            if (e.KeyCode == Keys.Space)
            {
                csp.is_moreselect = !csp.is_moreselect;
                csp.dict_cpoly.Clear();

                System.Drawing.Size screen = Screen.PrimaryScreen.Bounds.Size;
                this.Width = (int)(screen.Width * 0.8f);
                this.Height = (int)(screen.Height * 0.8f);
            }
        }
        private void Read_CsData_Click(object sender, EventArgs e)
        {
            if (csp.some_rects.Count == 0) return;
            var txtpye = sender.GetType();
            if (txtpye.Name == "Label")
            {
                groupBox2.Text = csp.namedatas[text_id];
            }
            else if (txtpye.Name == "TextBox")
            {
                text_id = Int32.Parse(Regex.Replace((sender as TextBox).Name, "[^0-9]", "")) - 1;
                cur_textBox.Text = csp.current_csdata.markdatas[text_id].ToString();
                groupBox2.Text = csp.namedatas[text_id];

                for (int i = 0; i < 9; i++)
                {
                    if (csp.current_csdata.markdatas[i] != 0)
                    {
                        if (csp.some_rects[csp.pickregion.index].maxdatas[i] == 0)
                        {
                            csp.some_rects[csp.pickregion.index].maxdatas[i] = csp.current_csdata.markdatas[i] * 2;
                        }
                        if (csp.some_rects[csp.pickregion.index].mindatas[i] == 0)
                        {
                            csp.some_rects[csp.pickregion.index].mindatas[i] = (float)Math.Floor((csp.current_csdata.markdatas[i] / 2));
                        }
                    }
                    else
                    {
                        csp.some_rects[csp.pickregion.index].mindatas[i] = 0;
                        csp.some_rects[csp.pickregion.index].maxdatas[i] = 0;
                    }
                    //Console.WriteLine(csp.pickregion.index+ " >>" + i+" " + "min: " + csp.some_rects[csp.pickregion.index].mindatas[i] +"cur: " + csp.current_csdata.markdatas[i] +"max: " + csp.some_rects[csp.pickregion.index].maxdatas[i]);
                 }
                
                max_textBox.Text = csp.some_rects[csp.pickregion.index].maxdatas[text_id].ToString();
                min_textBox.Text = csp.some_rects[csp.pickregion.index].mindatas[text_id].ToString();

            }
        }
        private void Write_CsData_TextChanged(object sender, EventArgs e)
        {
            float cur_value = 0;
            var txbox = sender as TextBox;
            if (cur_textBox.Text != "")
            {
                cur_value = float.Parse(cur_textBox.Text);
            }
            if (cur_value != 0)
            {
                if (txbox.Name.Contains("min") && min_textBox.Text != "")
                {
                    csp.some_rects[csp.pickregion.index].mindatas[text_id] = float.Parse(min_textBox.Text);
                    if (csp.some_rects[csp.pickregion.index].mindatas[text_id] >= cur_value)
                    {
                        txbox.Text = csp.some_rects[csp.pickregion.index].mindatas[text_id].ToString();
                    }
                }
                if (txbox.Name.Contains("max") && max_textBox.Text != "")
                {
                    csp.some_rects[csp.pickregion.index].maxdatas[text_id] = float.Parse(max_textBox.Text);
                    if (csp.some_rects[csp.pickregion.index].maxdatas[text_id] <= cur_value)
                    {
                        txbox.Text = csp.some_rects[csp.pickregion.index].maxdatas[text_id].ToString();
                    }
                }
            }
        }

        private void Only_Number_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txtBox = (TextBox)sender;
            //Regex regex = new Regex(@"^-?(\d+)?(\.)?(\d+)?$");
            Regex regex = new Regex(@"^(\d+)?(\.)?(\d+)?$");
            if (e.KeyChar != '\b')
            {
                string strInput = txtBox.Text;
                int SelectLength = txtBox.SelectionLength;//获取选中的字符长度 
                if (SelectLength == txtBox.Text.Length && txtBox.Text.Length != 0)//判断是否全部选中 
                {
                    strInput = e.KeyChar.ToString();
                }
                else
                {
                    if (e.KeyChar == '-')
                    {
                        if (txtBox.SelectionStart != 0)    e.Handled = true;
                        else  strInput = e.KeyChar.ToString() + strInput;
                    }
                    else   strInput = strInput + e.KeyChar.ToString();
                }
                if (!regex.IsMatch(strInput))  e.Handled = true;

            }
            if (e.KeyChar == 46)                 //小数点
            {
                if (txtBox.Text.Length <= 0)
                    e.Handled = true;           //小数点不能在第一位
                else
                {
                    float f;
                    float oldf;
                    bool b1 = false, b2 = false;
                    b1 = float.TryParse(txtBox.Text, out oldf);
                    b2 = float.TryParse(txtBox.Text + e.KeyChar.ToString(), out f);
                    if (b2 == false)
                    {
                        if (b1 == true)  e.Handled = true;
                        else   e.Handled = false;
                    }
                }
            }
        }

        private void cur_textBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (csp.current_csdata != null)
            {
                groupBox2.Text = csp.namedatas[text_id];
                cur_textBox.Text= csp.current_csdata.markdatas[text_id].ToString();
            }
        }

        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PolyClass.pick_irect = csp.pickregion;
            if (csp.some_rects.Count > 0) PolyClass.SaveDictPoly("dict_datas", csp.dict_cpoly);
            if (csp.some_rects.Count>0)  PolyClass.SaveSomeRects("rect_datas",csp.some_rects);
        }

        private void 读取ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            csp.is_loaddatas = true;
        }

        private void 放大ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            csp.is_zoomregion = true;
            csp.zoompos = new OpenCvSharp.Point(csp.frame_image.Width/2, csp.frame_image.Height / 2);
            if (csp.zoomraito < 4) csp.zoomraito += 0.5f;
        }

        private void 缩小ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            csp.is_zoomregion = true;
            csp.zoompos = new OpenCvSharp.Point(csp.frame_image.Width / 2, csp.frame_image.Height / 2);
            if (csp.zoomraito >= 1.5f) csp.zoomraito -= 0.5f;
        }
    }
}

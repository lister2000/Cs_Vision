using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using OpenCvSharp.Extensions;
using OpenCvSharp;
namespace Cs_Vision
{
	public class PolyFeature
	{
		public int region_index =-1;
		CsTools cst = CsTools.Instance;
		//CsPram csp = CsPram.Instance;
		public float poly_ratio = 0;
		public int poly_alike = 0;
		public Mat region_mat;
		public Mat poly_mat;
		public Rect poly_rect, region_rect;
		public RotatedRect poly_rbox;
		public OpenCvSharp.Point[] poly_gon;
		public OpenCvSharp.Point ploy_center   = new OpenCvSharp.Point();
		public OpenCvSharp.Point ploy_position = new OpenCvSharp.Point();
		public Point2f[] rbox_vertex = new Point2f[4];
		public int poly_width   = 0;  
		public int poly_height  = 0;  
		public float poly_angle = 0; 
		public int poly_circum  = 0;  //周长
		public int poly_area    = 0;  //面积
		public int poly_light   = 0;  //亮度
		public OpenCvSharp.Point line_pt0  = new OpenCvSharp.Point();
		public OpenCvSharp.Point line_pt1  = new OpenCvSharp.Point();
		public OpenCvSharp.Point line_pt2  = new OpenCvSharp.Point();
		public OpenCvSharp.Point gline_pt1 = new OpenCvSharp.Point();
		public OpenCvSharp.Point gline_pt2 = new OpenCvSharp.Point();
		public int line_distance= 0;  
		public float line_angle = 0;
		public int line_type = -1;
		public int line_id = -1;
		public float kmag = -1;
		public float kori = -1;
		public float[] kbin = new float[36];
		public PolyFeature()
		{
		}
		 public PolyFeature(Rect regionrect,int regionid, Mat ipoly, RotatedRect rbox, Mat regionmat)
		{
			region_mat = regionmat;
			region_rect = regionrect;
			region_index = regionid;
			poly_gon = new OpenCvSharp.Point[ipoly.Total()];
			for (int i = 0; i < ipoly.Total(); i++)
			{
				unsafe{ 
				int* p = (int*)ipoly.Ptr(i);
				poly_gon[i] = new OpenCvSharp.Point(p[0], p[1]);
				}
			}
			//Console.WriteLine("id:"+ regionid +" size:"+ poly_gon.Length);
			poly_rbox = rbox;
			rbox_vertex = poly_rbox.Points();
			ploy_center = (OpenCvSharp.Point)poly_rbox.Center;
			ploy_position = new OpenCvSharp.Point(regionrect.X + ploy_center.X, regionrect.Y + ploy_center.Y);
			poly_rect = Cv2.BoundingRect(ipoly);
			poly_mat = new Mat(regionmat, poly_rect);
			poly_area = (int)Cv2.ContourArea(ipoly);
			poly_ratio = (float)Math.Round((poly_rbox.Size.Width+ poly_rbox.Size.Height) / (regionrect.Width + regionrect.Height)*100,1);
			poly_width = (int)rbox.Size.Width;
			poly_height = (int)rbox.Size.Height;
			poly_angle = Math.Abs(rbox.Angle);
			poly_circum = (int)Cv2.ArcLength(ipoly, true);
			poly_light = PolyLight(new Mat(regionmat, poly_rect));
			//testHog(poly_mat);

			if (line_type == (int)CsPram.PICKTYPE.多边形边)
			{
				if (line_id < this.poly_gon.Length)
				{
					line_pt1 = this.poly_gon[line_id];
					line_pt2 = this.poly_gon[(line_id + 1) % this.poly_gon.Length];
					gline_pt1 = Add2Point(region_rect.TopLeft, line_pt1);
					gline_pt2 = Add2Point(region_rect.TopLeft, line_pt2);
				}
				else
				{
					Console.WriteLine("!!!!!!!!!!!多边形边错误!!!!!!!!!!!:" + line_id);
					line_id = this.poly_gon.Length - 1 > 0 ? this.poly_gon.Length - 1 : 0;
					DialogResult result = MessageBox.Show("多边形边错误", "提示");
				}

				line_distance = cst.Distance(line_pt1, line_pt2);
				if (line_pt1.Y > line_pt2.Y)
				{
					line_angle = cst.Angle2(line_pt1, line_pt2, 180);
					line_pt0 = line_pt1;
				}
				else
				{
					line_angle = cst.Angle2(line_pt2, line_pt1, 180);
					line_pt0 = line_pt2;
				}
				ploy_center = new OpenCvSharp.Point((line_pt1.X + line_pt2.X) / 2.0f, (line_pt1.Y + line_pt2.Y) / 2.0);
			}
		}

		OpenCvSharp.Point Add2Point(OpenCvSharp.Point a, OpenCvSharp.Point b)
		{
			return new OpenCvSharp.Point(a.X + b.X, a.Y + b.Y);
		}
		public void polyUpdate(PolyFeature curr, Mat mat)
		{
			region_index = curr.region_index;
			region_mat = mat;
			region_rect = curr.region_rect;
			poly_gon = curr.poly_gon;
			poly_rbox = curr.poly_rbox;
			rbox_vertex = poly_rbox.Points();
			ploy_center = curr.ploy_center;
			poly_rect = curr.poly_rect;
			poly_mat = curr.poly_mat;
			poly_area = curr.poly_area;
			poly_ratio = curr.poly_ratio;
			poly_width = curr.poly_width;
			poly_height = curr.poly_height;
			poly_angle = curr.poly_angle;
			poly_circum = curr.poly_circum;
			poly_light = curr.poly_light;
			//testHog(poly_mat);
			if (line_type == 1)//多边形边)
			{
				int inext = (line_id + 1) % curr.poly_gon.Length;
				if (line_id < curr.poly_gon.Length && inext< curr.poly_gon.Length)
				{
					line_pt1 = curr.poly_gon[line_id];
					line_pt2 = curr.poly_gon[(line_id + 1)% curr.poly_gon.Length];
					gline_pt1 = Add2Point(region_rect.TopLeft, line_pt1);
					gline_pt2 = Add2Point(region_rect.TopLeft, line_pt2);
                }
                else
                {
					Console.WriteLine("!!!!!!!!!!!多边形边id错误!!!!!!!!!!!:" + line_id);
					line_id = curr.poly_gon.Length - 1 > 0? curr.poly_gon.Length - 1:0;
				}

				line_distance = cst.Distance(line_pt1, line_pt2);
				if (line_pt1.Y > line_pt2.Y)
				{
					line_angle = cst.Angle2(line_pt1, line_pt2, 180);
					line_pt0 = line_pt1;
				}
				else
				{
					line_angle = cst.Angle2(line_pt2, line_pt1, 180);
					line_pt0 = line_pt2;
				}
				ploy_center = new OpenCvSharp.Point((line_pt1.X + line_pt2.X) / 2.0f, (line_pt1.Y + line_pt2.Y) / 2.0);
			}
			ploy_position = curr.ploy_position;
			ploy_position = new OpenCvSharp.Point(curr.region_rect.X + ploy_center.X, curr.region_rect.Y + ploy_center.Y);

		}
		int PolyLight(Mat mat)
		{
			Mat img = new Mat();
			float ivalue = 0;
			Cv2.Resize(mat, img, new OpenCvSharp.Size(8, 8));
			for (int r = 0; r < 8; r++)
			{
				for (int c = 0; c < 8; c++)
				{
					unsafe
					{
						byte* ptr = (byte*)img.Ptr(r, c);
						ivalue += ((ptr[0] + ptr[1] + ptr[2]) / 3);
					}
				}
			}
			return (int)(ivalue / 64);
		}
		unsafe void CalcuHOG(Mat dog, float[] bins)
		{
			int radio = dog.Cols / 2;
			OpenCvSharp.Point cpt = new OpenCvSharp.Point(radio, radio);
			for (int i = -radio; i <= radio; i++)
			{
				for (int j = -radio; j <= radio; j++)
				{
					float ori = 0; float mag = 0;
					int r = cpt.Y + i; int c = cpt.X + j;
					if (r > 0 && r < dog.Rows - 1 && c > 0 && c < dog.Cols - 1)
					{
						float w = (float)Math.Exp(-(i * i + j * j) / 4.5);//高斯加权公式
						float dx = ((byte*)dog.Ptr(r, c + 1))[0] - ((byte*)dog.Ptr(r, c - 1))[0];
						float dy = ((byte*)dog.Ptr(r + 1, c))[0] - ((byte*)dog.Ptr(r - 1, c))[0];
						mag = (float)Math.Sqrt(dx * dx + dy * dy) * w;
						ori = (float)Math.Atan(dy / dx);
					}
					int bin = ((int)(36 * (ori + Cv2.PI / 2.0) / Cv2.PI)) % 36;
					bins[bin] += mag;
				}
			}
			///return Point(kpt.x + cos(kori / 18.0 * CV_PI) * kmag * g_holesVal,
			///	kpt.y + sin(kori / 18.0 * CV_PI) * kmag * g_holesVal);
		}
		Mat MaxPooling(Mat mat)
		{
			Mat outmap = new Mat(mat.Rows / 2, mat.Cols / 2, mat.Type());
			for (int y = 0; y < mat.Rows - 1; y += 2)
			{
				for (int x = 0; x < mat.Cols - 1; x += 2)
				{
					unsafe
					{
						byte* map_ptr = (byte*)outmap.Ptr(y / 2, x / 2);
						byte* d1 = (byte*)mat.Ptr(y, x);
						byte* d2 = (byte*)mat.Ptr(y, x + 1);
						byte* d3 = (byte*)mat.Ptr(y + 1, x + 1);
						byte* d4 = (byte*)mat.Ptr(y + 1, x);
						map_ptr[0] = (byte)((d1[0] + d2[0] + d3[0] + d4[0]) / 4);
					}
				}
			}
			return outmap;
		}
		void TestHog(Mat bmat)
		{

			if (bmat.Data != null)
			{
				Mat hog = bmat;
				if (hog.Type() != MatType.CV_8UC1) Cv2.CvtColor(hog.Clone(), hog, ColorConversionCodes.BGR2GRAY);
				Cv2.Resize(hog, hog, new OpenCvSharp.Size(128, 128));
				hog = MaxPooling(hog);//64
				hog = MaxPooling(hog);//32
				hog = MaxPooling(hog);//16
				float[] bins = new float[36];

				CalcuHOG(hog, bins);

				for (int i = 1; i < 36; i++)
				{

					kbin[i] = bins[i];
					if (bins[i] > kmag)
					{
						kmag = bins[i]; kori = i;
					}
				}
				// Point kpt = ploy_center;
				// Point kpp = Point(kpt.x + cos(kori / 18.0 * CV_PI) * kmag,
				// kpt.y + sin(kori / 18.0 * CV_PI) *kmag);

			}
		}
	}
}





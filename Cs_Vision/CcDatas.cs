using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using OpenCvSharp.Extensions;
using OpenCvSharp;


namespace Cs_Vision
{
	[Serializable]
	public class CcDatas
	{
		public int count = 0;
		public int region_light = 0;
		public int obj2distance = 0;
		public float obj2angle = 0;
		public float[] markdatas = new float[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

		public PolyClass data_feat = new PolyClass();
		public CcDatas()
		{

		}
		public CcDatas(int icount, Rect irect, int light, PolyClass dc)
		{
			data_feat = dc;
			region_light = light;
			markdatas[0] = (irect.X + irect.Width / 2);          //横坐标
			markdatas[1] = 0;                                    //距离值 
			markdatas[2] = region_light;                         //亮度值
			markdatas[3] = (irect.Y + irect.Height / 2);         //纵坐标
			markdatas[4] = 0;                                    //面积值
			markdatas[5] = count = icount;                       //多边形
			markdatas[6] = 0;                                    //角度值
			markdatas[7] = irect.Width;                          //宽度值
			markdatas[8] = irect.Height;                         //高度值
		}
		public void UpdateDatas(int objs)
		{
			 markdatas[0] = (data_feat.poly_center.X);
			 if (objs == 2)
			 {
			 	markdatas[6] = (float)Math.Round(obj2angle, 2);
			 	markdatas[1] = obj2distance;
			 }
			 else
			 {
			 	if (data_feat.poly_type == (int)CsPram.PICKTYPE.旋转矩形) markdatas[6] = (float)Math.Round(data_feat.poly_rbox.Angle, 3);
			 	else markdatas[6] = (float)Math.Round(data_feat.edge_angle, 3);
			 	markdatas[1] = data_feat.edge_distance;
			 }
			 if (region_light != -1) markdatas[2] = region_light;
			 else markdatas[2] = region_light;
			 markdatas[3] = data_feat.poly_center.Y;
			 markdatas[4] = (int)Cv2.ContourArea(data_feat.poly_points); ;
			 markdatas[5] = count;
			 markdatas[7] = (int)data_feat.poly_rbox.Size.Width;
			 markdatas[8] = (int)data_feat.poly_rbox.Size.Height;
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

					//kbin[i] = bins[i];
					//if (bins[i] > kmag)
					//{
					//	kmag = bins[i]; kori = i;
					//}
				}
				// Point kpt = ploy_center;
				// Point kpp = Point(kpt.x + cos(kori / 18.0 * CV_PI) * kmag,
				// kpt.y + sin(kori / 18.0 * CV_PI) *kmag);

			}
		}
	}
}


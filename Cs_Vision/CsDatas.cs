using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using OpenCvSharp.Extensions;
using OpenCvSharp;


namespace Cs_Vision
{
  public class CsDatas
	{
		public int count = 0;
		public int region_light= 0;
		public int obj2distance= 0;
		public float obj2angle = 0 ;
		public float[] markdatas = new float[9];

		public PolyFeature data_feat = new PolyFeature();
		public CsDatas() {}
		public CsDatas(int icount, Rect irect, Mat imat,PolyFeature df)
        {
			data_feat = df;
			region_light = (int)RegionLight(imat);
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
			markdatas[0] = (data_feat.ploy_position.X);
			if (objs == 2)
			{
				markdatas[6] =(float)Math.Round(obj2angle, 2);
				markdatas[1] = obj2distance;
			}
			else
			{
				if (data_feat.line_type == (int)CsPram.PICKTYPE.旋转矩形) markdatas[6] = (float)Math.Round(data_feat.poly_angle, 3);
				else markdatas[6] = (float)Math.Round(data_feat.line_angle, 3);
				markdatas[1] = data_feat.line_distance;
			}
			if (data_feat.poly_light != -1) markdatas[2] = data_feat.poly_light;
			else markdatas[2] =region_light;
			markdatas[3] = data_feat.ploy_position.Y;
			markdatas[4] = data_feat.poly_area;
			markdatas[5] = count;
			markdatas[7] = data_feat.poly_width;
			markdatas[8] = data_feat.poly_height;
		}
		float RegionLight(Mat mat)
		{
			float ivalue = 0;
			Mat img = new Mat();
			Cv2.Resize(mat, img, new OpenCvSharp.Size(8, 8));
			for (int r = 0; r < 8; r++)
			{
				for (int c = 0; c < 8; c++)
				{
                    unsafe{
						byte* ptr = (byte*)img.Ptr(r, c);
						ivalue += ((ptr[0] + ptr[1] + ptr[2]) / 3);
                    }
				}
			}
			return (ivalue / 64);
		}

	}

}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
namespace Cs_Vision
{
	class ColorDetector
	{
		//https://www.cnblogs.com/hnzsb-vv1130/p/6587097.html
		
		int minDist;//最小可接受距离
		
		Vec3b target;//目标色
		
		Mat retmat;//结果图像

		//计算与目标颜色的距离
		unsafe int getDistance(byte* color)
		{
			return Math.Abs(color[0] - target[0]) + Math.Abs(color[1] - target[1]) + Math.Abs(color[2] - target[2]);
		}

		//空构造函数
		public ColorDetector()
		{
			//初始化默认参数
			target[0] = target[1] = target[2] = 0;
		}

		//设置色彩距离阈值，阈值必须是正的，否则设为0
		public void setColorDistanceThreshold(int distance)
		{
			if (distance < 0)
				distance = 0;
			minDist = distance;
		}

		//获取色彩距离阈值
		public int getColorDistanceThreshold()
		{
			return minDist;
		}

		//设置需检测的颜色
		public void setTargetColor( byte red, byte green, byte blue)
		{
			//BGR顺序
			target[2] = red;
			target[1] = green;
			target[0] = blue;
		}

		//设置需检测的颜色
		public void setTargetColor(Vec3b color)
		{
			target = color;
		}

		//获取需检测的颜色
		Vec3b getTargetColor()
		{
			return target;
		}
	 unsafe public	Mat process(Mat image)//核心的处理方法
	{
		//按需重新分配二值图像
		//与输入图像的尺寸相同，但是只有一个通道
		retmat = new Mat(image.Rows, image.Cols, MatType.CV_8U);

			//得到迭代器
            for (int r = 0; r < image.Rows; r++)
            {
                for (int c = 0; c < image.Cols; c++)
                {
					byte* input = (byte*)image.Ptr(r,c);
					byte* output = (byte*)retmat.Ptr(r,c);

					//计算离目标颜色的距离
					if (getDistance(input) < minDist)
					{
						output[0] = 255;
					}
					else
					{
						output[0] = 0;
					}
				}
            }
		return retmat;
	}
}
}







using System;
using System.Collections.Generic;
using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using OpenCvSharp.Extensions;
//using OpenCvSharp.Dnn;
using OpenCvSharp;
using OpenCvSharp.XFeatures2D;

namespace Cs_Vision
{
	public class CsTools
	{
		CsPram csp = CsPram.Instance;
		private static CsTools instance = new CsTools();
		//private CsTools() { }
		public static CsTools Instance
		{
			get { return instance; }
			set { instance = value; }
		}
		public float Angle2(OpenCvSharp.Point c, OpenCvSharp.Point p, int type)
		{
			float dx = c.X - p.X;
			float dy = c.Y - p.Y;

			float dr = (float)Math.Sqrt(dx * dx + dy * dy);
			float angle = (float)(Math.Asin(dx / dr) + Cv2.PI / 2);

			if (type == 360)//0	-> 360
			{
				if (dy < 0) angle = 2.0f * (float)Cv2.PI - angle;
			}
			else
			{
				if (dy < 0) angle = -angle;//-180 -> 180
			}
			angle = angle / (float)Cv2.PI * 180.0f;
			//angle = (float)Math.Round(angle, 2);

			return angle;
		}
		float Angle3(OpenCvSharp.Point cen, OpenCvSharp.Point first, OpenCvSharp.Point second)
		{
			float dx1, dx2, dy1, dy2;
			float angle;
			dx1 = first.X - cen.X;
			dy1 = first.Y - cen.Y;
			dx2 = second.X - cen.X;
			dy2 = second.Y - cen.Y;
			float c = (float)Math.Sqrt(dx1 * dx1 + dy1 * dy1) * (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);
			if (c == 0) return -1;
			angle = (float)Math.Acos((dx1 * dx2 + dy1 * dy2) / c);
			return angle / (float)Cv2.PI * 180.0f; ;
		}
		public int Distance(OpenCvSharp.Point pointO, OpenCvSharp.Point pointA)
		{
			return (int)Math.Sqrt(Math.Pow((pointO.X - pointA.X), 2) + Math.Pow((pointO.Y - pointA.Y), 2));
		}
		float TriangleArea(float v1x, float v1y, float v2x, float v2y, float v3x, float v3y)
		{
			//S = (1x2y + 2x3y + 3x1y - 1y2x - 2y3x - 3y1) * 1 / 2 //以第一点坐标开始口诀122331-122331
			return Math.Abs((v1x * v2y + v2x * v3y + v3x * v1y - v1y * v2x - v2y * v3x - v3y * v1x) / 2.0f);
		}
		float TriangleArea(OpenCvSharp.Point p1, OpenCvSharp.Point p2, OpenCvSharp.Point p3)
		{
			return Math.Abs((p1.X * p2.Y + p2.X * p3.Y + p3.X * p1.Y - p1.Y * p2.X - p2.Y * p3.X - p3.Y * p1.X) / 2.0f);
		}
		bool isInTriangle(OpenCvSharp.Point point, OpenCvSharp.Point v0, OpenCvSharp.Point v1, OpenCvSharp.Point v2)
		{

			float t = TriangleArea(v0.X, v0.Y, v1.X, v1.Y, v2.X, v2.Y);
			float a = TriangleArea(v0.X, v0.Y, v1.X, v1.Y, point.X, point.Y) +
				TriangleArea(v0.X, v0.Y, point.X, point.Y, v2.X, v2.Y) +
				TriangleArea(point.X, point.Y, v1.X, v1.Y, v2.X, v2.Y);
			if (Math.Abs(t - a) <= 0.01f)
				return true;
			else
				return false;

		}
		public bool InRectangle(OpenCvSharp.Point point, OpenCvSharp.Point v0, OpenCvSharp.Point v1, OpenCvSharp.Point v2, OpenCvSharp.Point v3)
		{

			float S1 = TriangleArea(v0.X, v0.Y, v1.X, v1.Y, v2.X, v2.Y) * 2.0f;
			float S2 = TriangleArea(v0.X, v0.Y, v1.X, v1.Y, point.X, point.Y) +
				TriangleArea(v1.X, v1.Y, v2.X, v2.Y, point.X, point.Y) +
				TriangleArea(v2.X, v2.Y, v3.X, v3.Y, point.X, point.Y) +
				TriangleArea(v0.X, v0.Y, v3.X, v3.Y, point.X, point.Y);
			if (Math.Abs(S1 - S2) <= 0.01f)
				return true;
			else
				return false;

		}
		public bool InRectangle(OpenCvSharp.Point point, Rect rect)
		{
			OpenCvSharp.Point v0 = rect.TopLeft;
			OpenCvSharp.Point v1 = new OpenCvSharp.Point(rect.X + rect.Width, rect.Y);
			OpenCvSharp.Point v2 = rect.BottomRight;
			OpenCvSharp.Point v3 = new OpenCvSharp.Point(rect.X, rect.Y + rect.Height);
			float S1 = TriangleArea(v0.X, v0.Y, v1.X, v1.Y, v2.X, v2.Y) * 2.0f;
			float S2 = TriangleArea(v0.X, v0.Y, v1.X, v1.Y, point.X, point.Y) +
				TriangleArea(v1.X, v1.Y, v2.X, v2.Y, point.X, point.Y) +
				TriangleArea(v2.X, v2.Y, v3.X, v3.Y, point.X, point.Y) +
				TriangleArea(v0.X, v0.Y, v3.X, v3.Y, point.X, point.Y);
			if (Math.Abs(S1 - S2) <= 0.01f)
				return true;
			else
				return false;

		}
		public Rect Urect(Rect r1, Rect r2)
		{
			Rect intersection = r1 & r2; // 计算矩形交集

			return intersection;
#if false
			OpenCvSharp.Point start = new OpenCvSharp.Point();
			OpenCvSharp.Point ended = new OpenCvSharp.Point();
			start.X = r1.X > r2.X ? r1.X : r2.X;
			start.Y = r1.Y > r2.Y ? r1.Y : r2.Y;
			ended.X = r1.BottomRight.X < r2.BottomRight.X ? r1.BottomRight.X : r2.BottomRight.X;
			ended.Y = r1.BottomRight.Y < r2.BottomRight.Y ? r1.BottomRight.Y : r2.BottomRight.Y;
			if (start.X > ended.X || start.Y > ended.Y)
			{
				Console.WriteLine("Urect error!");
				return new Rect();
			}
			else
			{
				if (ended.X - start.X < r1.Width)
				{
					if (r1.X < r2.Width / 2)
					{
						start.X = 0;
						ended.X = r1.Width;
					}
					else
					{
						start.X = r2.Width - r1.Width;
						ended.X = r2.Width;
					}
				}
				if (ended.Y - start.Y < r1.Height)
				{
					if (r1.Y < r1.Height / 2)
					{
						start.Y = 0;
						ended.Y = r1.Height;
					}
					else
					{
						start.Y = r2.Height - r1.Height;
						ended.Y = r2.Height;
					}
				}
				return new Rect(start.X, start.Y, ended.X - start.X, ended.Y - start.Y);
			}
#endif
		}
		float HsvHist(Mat bgr_src, Mat bgr_test)
		{
			//OpenCVSharp 4.5 直方图对比
			//https://blog.csdn.net/jimtien/article/details/118296315
			Mat hsv_base = new Mat();
			Mat hsv_test1 = new Mat();
			Cv2.CvtColor(bgr_src, hsv_base, ColorConversionCodes.BGR2HSV);
			Cv2.CvtColor(bgr_test, hsv_test1, ColorConversionCodes.BGR2HSV);
			int h_bins = 50, s_bins = 60;
			int[] histSize = { h_bins, s_bins };
			float[] h_ranges = { 0, 180 };
			float[] s_ranges = { 0, 256 };
			float[][] ranges = { h_ranges, s_ranges };
			int[] channels = { 0, 1 };
			Mat hist_base = new Mat();
			Mat hist_test1 = new Mat();
			Cv2.CalcHist(new Mat[] { hsv_base }, channels, null, hist_base, 2, histSize, ranges, true, false);
			Cv2.Normalize(hist_base, hist_base, 0, 1, NormTypes.MinMax, -1, null);
			Cv2.CalcHist(new Mat[] { hsv_test1 }, channels, null, hist_test1, 2, histSize, ranges, true, false);
			Cv2.Normalize(hist_test1, hist_test1, 0, 1, NormTypes.MinMax, -1, null);
			return (float)Cv2.CompareHist(hist_base, hist_test1, HistCompMethods.Correl);
		}
		public bool TestScore(PolyFeature last, PolyFeature curr, int limit)
		{
			int score = 0;
            if (last.region_index == curr.region_index)
            {

            }
			float alike_hist = HsvHist(last.poly_mat, curr.poly_mat);
			if (alike_hist > 0.8f)score += 20;

			float alike_light = Math.Abs(last.poly_light - curr.poly_light);
			if (Math.Abs(last.poly_light - curr.poly_light) < 8)score += 10;

			float alike_area = Math.Abs((last.poly_area - curr.poly_area) / (float)last.poly_area);
			if (Math.Abs((last.poly_area - curr.poly_area) / (float)last.poly_area) < 0.01)score += 20;
	
			float alike_circum = Math.Abs(last.poly_circum - curr.poly_circum) / (float)last.poly_circum;
			if (Math.Abs(last.poly_circum - curr.poly_circum) / (float)last.poly_circum < 0.01)score += 20;


			float alike_width = Math.Abs(curr.poly_rbox.Size.Width - last.poly_rbox.Size.Width) / (float)last.poly_rbox.Size.Width;
			float alike_height = Math.Abs(curr.poly_rbox.Size.Height - last.poly_rbox.Size.Height) / (float)last.poly_rbox.Size.Height;
			if (alike_width < 0.05 && alike_height < 0.05)score += 10;

			float alike_size = Math.Abs(last.poly_gon.Length - curr.poly_gon.Length);
			if (alike_size == 0)score += 20;

			curr.poly_alike = score;

			return score > limit;
		}
		public void oneCross(Mat img, OpenCvSharp.Point pt, Scalar cl, int zm)
		{
			Cv2.Line(img, new OpenCvSharp.Point(pt.X - zm * 4, pt.Y), new OpenCvSharp.Point(pt.X + zm * 4, pt.Y), cl, zm);
			Cv2.Line(img, new OpenCvSharp.Point(pt.X, pt.Y - zm * 4), new OpenCvSharp.Point(pt.X, pt.Y + zm * 4), cl, zm);
		}
		OpenCvSharp.Point CrossPoint(Point2f a1, Point2f a2, Point2f b1, Point2f b2)
		{
			float ka, kb;
			Vec4f LineA = new Vec4f(a1.X, a1.Y, a2.X, a2.Y);
			Vec4f LineB = new Vec4f(b1.X, b1.Y, b2.X, b2.Y);
			ka = (LineA[3] - LineA[1]) / (float)(LineA[2] - LineA[0]); //求出LineA斜率
			kb = (LineB[3] - LineB[1]) / (float)(LineB[2] - LineB[0]); //求出LineB斜率

			Point2f crossPoint = new Point2f();
			crossPoint.X = (ka * LineA[0] - LineA[1] - kb * LineB[0] + LineB[1]) / (ka - kb);
			crossPoint.Y = (ka * kb * (LineA[0] - LineB[0]) + ka * LineB[1] - kb * LineA[1]) / (ka - kb);
			//Console.WriteLine("ka: "+ ka + " kb: " + kb + " icross: " + crossPoint);
			return (OpenCvSharp.Point)crossPoint;
		}
		public void TestMeasure(Mat region_mat, Mat region_mask,int region_index)
		{
			Mat[] contours;
			Mat hierarchy = new Mat();
			List<PolyFeature> curr_feats = new List<PolyFeature>();
			Cv2.FindContours(region_mask, out contours, hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
			Rect region_rect = csp.some_rects[region_index].rect;
			for (int i = 0; i < contours.Length; i++)
			{
				RotatedRect rbox = Cv2.MinAreaRect(contours[i]);
				float rarea = (rbox.Size.Width + rbox.Size.Height) / (region_mask.Rows + region_mask.Cols) * 100;
				if (rarea > csp.areasMin  && rarea < csp.areasMax)
				{
					Mat poly = contours[i].ApproxPolyDP(csp.asideVal, true);
					PolyFeature pfeat = new PolyFeature(region_rect,region_index, poly, rbox, region_mat);
					if (curr_feats.Count < 9)
					{
						curr_feats.Add(pfeat);
                        if (!csp.is_moreselect)
                        {
							Cv2.Polylines(region_mat, poly, true, csp.yellow_color, csp.izoomVal);
							Cv2.Circle(region_mat, pfeat.poly_gon[0], csp.izoomVal * 2, csp.yellow_color, csp.izoomVal);
						}
                        else
                        {
							Point2f[] P = rbox.Points();
							for (int p = 0; p < P.Length; p++)Cv2.Line(region_mat, (OpenCvSharp.Point)P[p], (OpenCvSharp.Point)P[(p + 1) % 4], csp.yellow_color, csp.izoomVal);
						}
					}
				}
			}
			if (curr_feats.Count <= 0) return;

			//选择数据
			if (csp.is_mouseclick && InRectangle(csp.curr_point, region_rect))
			{
				csp.is_mouseclick = false;//一次
				OpenCvSharp.Point local_point = new OpenCvSharp.Point(csp.curr_point.X - region_rect.X, csp.curr_point.Y - region_rect.Y);
				for (int i = 0; i < curr_feats.Count; i++)
				{
					//触发选择
					int mark_id = -1;
					if (!csp.is_moreselect)
					{
						for (int vex_id = 0; vex_id < curr_feats[i].poly_gon.Length; vex_id++)
						{
							int next_id = (vex_id + 1) % curr_feats[i].poly_gon.Length;
							if (Angle3(local_point, curr_feats[i].poly_gon[vex_id], curr_feats[i].poly_gon[next_id]) > 160)
							{
								curr_feats[i].line_type = (int)CsPram.PICKTYPE.多边形边;
								curr_feats[i].line_id = mark_id = vex_id;
								break;
							}
						}
					}
					else
					{
						Point2f[] vertex2f = curr_feats[i].poly_rbox.Points();
						for (int bx = 0; bx < 4; bx++)
						{
							if (Angle3(local_point, (OpenCvSharp.Point)vertex2f[bx], (OpenCvSharp.Point)vertex2f[(bx + 1) % 4]) > 170)
							{
								curr_feats[i].line_type = (int)CsPram.PICKTYPE.旋转矩形;
								curr_feats[i].line_id = mark_id = 0;
								break;
							}
						}
					}
					//删除重复
					if (mark_id != -1)
					{
						for (int old = csp.dict_polys.Count - 1; old >= 0; old--)
						{
							if (csp.dict_polys[old].index == region_index)
							{
								if (TestScore(csp.dict_polys[old].feat, curr_feats[i], csp.alike60))
								{
									if (csp.dict_polys[old].feat.line_id == mark_id)
									{
										csp.dict_polys.RemoveAt(old);
										mark_id = -1;
										break;
									}
								}
							}
						}
					}
					if (mark_id > -1)
					{
						csp.dict_polys.Add(new poly_feture(curr_feats[i]));
						break;
					}
				}
			}
            if (false)
            {
				curr_feats.Sort(delegate (PolyFeature x, PolyFeature y)
				{
					if (x.poly_area > y.poly_area) return -1; else return 1;
				});
				for (int i = 0; i < curr_feats.Count; i++)
				{
					if (csp.dict_polys.Count == 0) Cv2.PutText(csp.result_image, ("" + i),
						 new OpenCvSharp.Point(curr_feats[i].ploy_center.X + region_rect.X, curr_feats[i].ploy_center.Y + region_rect.Y),
						 HersheyFonts.HersheyPlain, csp.izoomVal, csp.green_color, csp.izoomVal);
				}
			}

			//打包数据
			CsDatas csdata = new CsDatas(curr_feats.Count, region_rect, region_mat, curr_feats[0]);
			
			if (csp.dict_polys.Count > 0)
			{
				//标记选择
				PolyFeature pick_feat =  new PolyFeature();
				for (int i = 0; i < csp.dict_polys.Count; i++)
				{
					if (region_index == csp.dict_polys[i].index)
					{
						pick_feat = csp.dict_polys[i].feat; 
						PolyFeature dict_feat = csp.dict_polys[i].feat;
						csdata.data_feat = csp.dict_polys[i].feat;
						if (dict_feat.line_type == (int)CsPram.PICKTYPE.多边形边 && !csp.is_moreselect)
						{
							//Mat mat = new Mat(region_mat.Size(), MatType.CV_8UC1);
							//List<OpenCvSharp.Point[]> npts = new List<OpenCvSharp.Point[]>();
							//npts.Add(dict_feat.poly_gon);
							//Cv2.FillPoly(mat, npts, new Scalar(255,0,0));
							//Mat[] channels = Cv2.Split(region_mat);
							//channels[0] += mat;
							//Cv2.Merge(channels, region_mat);
							int next_id = (dict_feat.line_id + 1) % dict_feat.poly_gon.Length;
							Cv2.Line(region_mat, dict_feat.poly_gon[dict_feat.line_id], dict_feat.poly_gon[next_id], csp.blue_150, csp.izoomVal*2);
						}
						else if (dict_feat.line_type == (int)CsPram.PICKTYPE.旋转矩形 && csp.is_moreselect)
						{
							for (int b = 0; b < 4; b++)
								Cv2.Line(region_mat, (OpenCvSharp.Point)dict_feat.rbox_vertex[b], (OpenCvSharp.Point)dict_feat.rbox_vertex[(b + 1) % 4], csp.blue_150, csp.izoomVal * 2);
						}
					}
				}

				//画出选择
				if (pick_feat.poly_area > csp.AREA3600)
				{
					if (pick_feat.line_type == (int)CsPram.PICKTYPE.多边形边 && !csp.is_moreselect)
					{
						int s_id0 = pick_feat.line_id;
						int s_id1 = (pick_feat.line_id + 1)% pick_feat.poly_gon.Length;
						Cv2.Line(region_mat, pick_feat.poly_gon[s_id0], pick_feat.poly_gon[s_id1], csp.blue_color, csp.izoomVal);
					}
					else if (pick_feat.line_type == (int)CsPram.PICKTYPE.旋转矩形 && csp.is_moreselect)
					{
						for (int b = 0; b < 4; b++)
						{
							Cv2.Line(region_mat, (OpenCvSharp.Point)pick_feat.rbox_vertex[b], (OpenCvSharp.Point)pick_feat.rbox_vertex[(b + 1) % 4], csp.blue_color, csp.izoomVal);
						}
					}
				}
				
				//编辑信息
				if (csp.dict_polys.Count == 2)
				{
					float agl = 0;
                    Cv2.Line(csp.result_image, csp.dict_polys[0].feat.ploy_position, csp.dict_polys[1].feat.ploy_position, csp.cyan_color, csp.izoomVal);
                    if (!csp.is_moreselect)
                    {
						if (csp.dict_polys[0].feat.line_type == (int)CsPram.PICKTYPE.多边形边 && csp.dict_polys[1].feat.line_type == (int)CsPram.PICKTYPE.多边形边)
						{
							OpenCvSharp.Point icorss = CrossPoint(csp.dict_polys[0].feat.gline_pt1, csp.dict_polys[0].feat.gline_pt2,csp.dict_polys[1].feat.gline_pt1, csp.dict_polys[1].feat.gline_pt2);
							agl = Angle3(icorss, csp.dict_polys[0].feat.ploy_position, csp.dict_polys[1].feat.ploy_position);
							Cv2.Circle(csp.result_image, icorss, csp.izoomVal * 5, csp.gray_200, csp.izoomVal);
							Cv2.Line(csp.result_image, csp.dict_polys[0].feat.ploy_position, icorss, csp.gray_200, csp.izoomVal);
							Cv2.Line(csp.result_image, csp.dict_polys[1].feat.ploy_position, icorss, csp.gray_200, csp.izoomVal);
						}
					}
					if (csp.is_moreselect)
                    {
						if (csp.dict_polys[0].feat.line_type == (int)CsPram.PICKTYPE.旋转矩形 || csp.dict_polys[1].feat.line_type == (int)CsPram.PICKTYPE.旋转矩形)
						{
							if (csp.dict_polys[0].feat.ploy_position.Y > csp.dict_polys[1].feat.ploy_position.Y)
								agl = Angle2(csp.dict_polys[0].feat.ploy_position, csp.dict_polys[1].feat.ploy_position, 180);
							else
								agl = Angle2(csp.dict_polys[1].feat.ploy_position, csp.dict_polys[0].feat.ploy_position, 180);
						}
					}
					csdata.obj2angle = agl;
					csdata.obj2distance = Distance(csp.dict_polys[0].feat.ploy_position, csp.dict_polys[1].feat.ploy_position);
                }

				//填充数据
				csdata.UpdateDatas(csp.dict_polys.Count);
			}
			csp.some_rects[region_index].csdata = csdata;

			//更新选择
			for (int n = 0; n < csp.dict_polys.Count; n++)
			{
				if (csp.dict_polys[n].rect.Size == region_rect.Size)
				{
					for (int m = 0; m < curr_feats.Count; m++)
					{
						if (TestScore(csp.dict_polys[n].feat, curr_feats[m], csp.alike60))
						{
							if (csp.dict_polys.Count * curr_feats.Count > 0)
							{
								csp.dict_polys[n].feat.polyUpdate(curr_feats[m], region_mat);
							}
							else
							{
								//线程之间数据不同步引起"更新数据错误!!!";
							}
						}
						// 	Cv2.PutText(csp.result_image,("%" + curr_feats[m].poly_ratio), curr_feats[m].ploy_position,
					}
				}
			}
		}

		static int minHessian = 400;
		SURF detector = SURF.Create(minHessian);
		KeyPoint[] keypoints1, keypoints2;
		Mat descriptor1 = new Mat(), descriptor2 = new Mat();
		//https://blog.csdn.net/jimtien/article/details/118788900
		public void TestSURF(Mat img1,Mat img2)
		{
			if (img1.Empty() || img2.Empty())
			{
				Console.WriteLine("输入图像不存在");
			}
			//第一步：用 SURF Detector 检测关键点，然后计算特征描述向量

			detector.DetectAndCompute(img1, null, out keypoints1, descriptor1);
			detector.DetectAndCompute(img2, null, out keypoints2, descriptor2);

			//第二步：用基于 FLANN 的方法匹配特征描述向量
			DescriptorMatcher matcher = DescriptorMatcher.Create("FlannBased");
			DMatch[][] matches = matcher.KnnMatch(descriptor1, descriptor2, 2);

			//用 lowe 比法则过滤匹配结果
			double ratio_thresh = .5;
			List<DMatch> good_matches = new List<DMatch>();
			for (int i = 0; i < matches.Length; i++)
			{
				if (matches[i][0].Distance < ratio_thresh * matches[i][1].Distance)
				{
					good_matches.Add(matches[i][0]);
				}
			}
			//第三步：画出匹配线
			Mat image_matches = new Mat();
			Cv2.DrawMatches(img1, keypoints1, img2, keypoints2, good_matches, image_matches,
				null, null, null, DrawMatchesFlags.NotDrawSinglePoints);

			//第四步：锚定物体
			Point2d[] obj = new Point2d[good_matches.Count()], scene = new Point2d[good_matches.Count()];
			for (int i = 0; i < good_matches.Count(); i++)
			{
				obj[i] = keypoints1[good_matches[i].QueryIdx].Pt.ToPoint();
				scene[i] = keypoints2[good_matches[i].TrainIdx].Pt.ToPoint();
				//Cv2.Circle(csp.result_image, (Point)scene[i], csp.izoomVal , csp.cyan_color, csp.izoomVal);
			}
           // if (obj.Length>3 && scene.Length>3)
            {
            try
            {
				Mat H = Cv2.FindHomography(obj, scene, HomographyMethods.Ransac, 3, null);
				Point2f[] obj_corners = new Point2f[4];
				obj_corners[0] = new OpenCvSharp.Point(0, 0);
				obj_corners[1] = new OpenCvSharp.Point((float)img1.Cols, 0);
				obj_corners[2] = new OpenCvSharp.Point((float)img1.Cols, (float)img1.Rows);
				obj_corners[3] = new OpenCvSharp.Point(0, (float)img1.Rows);

				Point2f[] scene_corners = Cv2.PerspectiveTransform(obj_corners, H);
				// Point p1 = new Point(scene_corners[0].X + img1.Cols, scene_corners[0].Y);
				// Point p2 = new Point(scene_corners[1].X + img1.Cols, scene_corners[1].Y);
				// Cv2.Line(image_matches, p1, p2, new Scalar(0, 255, 0), 4);
				// p1 = new Point(scene_corners[1].X + img1.Cols, scene_corners[1].Y);
				// p2 = new Point(scene_corners[2].X + img1.Cols, scene_corners[2].Y);
				// Cv2.Line(image_matches, p1, p2, new Scalar(0, 255, 0), 4);
				// 
				// p1 = new Point(scene_corners[2].X + img1.Cols, scene_corners[2].Y);
				// p2 = new Point(scene_corners[3].X + img1.Cols, scene_corners[3].Y);
				// Cv2.Line(image_matches, p1, p2, new Scalar(0, 255, 0), 4);
				// 
				// p1 = new Point(scene_corners[3].X + img1.Cols, scene_corners[3].Y);
				// p2 = new Point(scene_corners[0].X + img1.Cols, scene_corners[0].Y);
				// Cv2.Line(image_matches, p1, p2, new Scalar(0, 0, 255), 4);
				
				 csp.pickregion.region.X = (int)scene_corners[0].X;
				 csp.pickregion.region.Y = (int)scene_corners[0].Y;

                for (int i = 0; i < 4; i++)
                {
					Cv2.Line(csp.result_image, (OpenCvSharp.Point)scene_corners[i], (OpenCvSharp.Point)scene_corners[(i+1)%4], new Scalar(0, 0, 255), 12);

				}
				}
            catch
			{
				//Console.WriteLine("");
			}

    
           //第五步：画出锚定物体

		    Cv2.NamedWindow("Matches", WindowFlags.Normal);
			Cv2.ImShow("Matches", image_matches);

            }
   
		}

		//https://www.freesion.com/article/31671543664/
		//mat1是用来存九个像素坐标的，mat2用来存机器人坐标
		public Mat VectorToHomMat2d(Point2d[] calib_img_pixel_coordinates, Point2d[] calib_img_rob_coordinates)
		{
			if (calib_img_pixel_coordinates.Length != 9 && calib_img_rob_coordinates.Length != 9)
			{
				return null;
			}

			Mat mat1 = new Mat(9, 2, MatType.CV_64F); //这里MatType的解释我之前博客里有介绍
			mat1.Set<double>(0, 0, calib_img_pixel_coordinates[0].X);
			mat1.Set<double>(0, 1, calib_img_pixel_coordinates[0].Y);
			mat1.Set<double>(1, 0, calib_img_pixel_coordinates[1].X);
			mat1.Set<double>(1, 1, calib_img_pixel_coordinates[1].Y);
			mat1.Set<double>(2, 0, calib_img_pixel_coordinates[2].X);
			mat1.Set<double>(2, 1, calib_img_pixel_coordinates[2].Y);
			mat1.Set<double>(3, 0, calib_img_pixel_coordinates[3].X);
			mat1.Set<double>(3, 1, calib_img_pixel_coordinates[3].Y);
			mat1.Set<double>(4, 0, calib_img_pixel_coordinates[4].X);
			mat1.Set<double>(4, 1, calib_img_pixel_coordinates[4].Y);
			mat1.Set<double>(5, 0, calib_img_pixel_coordinates[5].X);
			mat1.Set<double>(5, 1, calib_img_pixel_coordinates[5].Y);
			mat1.Set<double>(6, 0, calib_img_pixel_coordinates[6].X);
			mat1.Set<double>(6, 1, calib_img_pixel_coordinates[6].Y);
			mat1.Set<double>(7, 0, calib_img_pixel_coordinates[7].X);
			mat1.Set<double>(7, 1, calib_img_pixel_coordinates[7].Y);
			mat1.Set<double>(8, 0, calib_img_pixel_coordinates[8].X);
			mat1.Set<double>(8, 1, calib_img_pixel_coordinates[8].Y);

			Mat mat2 = new Mat(9, 2, MatType.CV_64F);
			mat2.Set<double>(0, 0, calib_img_rob_coordinates[0].X);
			mat2.Set<double>(0, 1, calib_img_rob_coordinates[0].Y);
			mat2.Set<double>(1, 0, calib_img_rob_coordinates[1].X);
			mat2.Set<double>(1, 1, calib_img_rob_coordinates[1].Y);
			mat2.Set<double>(2, 0, calib_img_rob_coordinates[2].X);
			mat2.Set<double>(2, 1, calib_img_rob_coordinates[2].Y);
			mat2.Set<double>(3, 0, calib_img_rob_coordinates[3].X);
			mat2.Set<double>(3, 1, calib_img_rob_coordinates[3].Y);
			mat2.Set<double>(4, 0, calib_img_rob_coordinates[4].X);
			mat2.Set<double>(4, 1, calib_img_rob_coordinates[4].Y);
			mat2.Set<double>(5, 0, calib_img_rob_coordinates[5].X);
			mat2.Set<double>(5, 1, calib_img_rob_coordinates[5].Y);
			mat2.Set<double>(6, 0, calib_img_rob_coordinates[6].X);
			mat2.Set<double>(6, 1, calib_img_rob_coordinates[6].Y);
			mat2.Set<double>(7, 0, calib_img_rob_coordinates[7].X);
			mat2.Set<double>(7, 1, calib_img_rob_coordinates[7].Y);
			mat2.Set<double>(8, 0, calib_img_rob_coordinates[8].X);
			mat2.Set<double>(8, 1, calib_img_rob_coordinates[8].Y);

			Mat Hom_mat2d = Cv2.EstimateAffine2D(mat1, mat2);

			return Hom_mat2d;
		}
		//下面的方法是将像素坐标转为机器人坐标
		public Point2d AffineTransPoint2d(Mat Hom_mat2d, Point2d image_coordinates)
		{
			Point2d robot_coordinate;
			var A = Hom_mat2d.Get<double>(0, 0); //这里不能像opencv那样用Mat.ptr(),不然读出来的数会大的离谱，具体原因我也不清楚，有知道的伙伴欢迎评论讨论。
			var B = Hom_mat2d.Get<double>(0, 1);
			var C = Hom_mat2d.Get<double>(0, 2);
			var D = Hom_mat2d.Get<double>(1, 0);
			var E = Hom_mat2d.Get<double>(1, 1);
			var F = Hom_mat2d.Get<double>(1, 2);

			Console.WriteLine(A);

			robot_coordinate.X = (A * image_coordinates.X) + (B * image_coordinates.Y) + C;
			robot_coordinate.Y = (D * image_coordinates.X) + (E * image_coordinates.Y) + F;
			return robot_coordinate;
		}

		public void TestStitcher()
		{
			//https://blog.csdn.net/jimtien/article/details/119033378
			//https://blog.csdn.net/tfarcraw/article/details/113920612
			Stitcher.Mode mode = Stitcher.Mode.Scans;

			string folderName = @"Datas\";
			string[] imageFiles = { "1.jpg", "2.jpg", "3.jpg", "4.jpg", "5.jpg" };
			//string folderName = @"Datas\";
			//string[] imageFiles = { "11.png", "12.png", "13.png", "14.png", "15.png" };

			string result_name = "result.jpg";
			Mat[] imgs = new Mat[imageFiles.Length];

			//读入图像
			for (int i = 0; i < imageFiles.Length; i++)
			{
				imgs[i] = new Mat(folderName + imageFiles[i], ImreadModes.Color);
			}

			Mat pano = new Mat();

			Stitcher stitcher = Stitcher.Create(mode);
			Stitcher.Status status = stitcher.Stitch(imgs, pano);
			if (status != Stitcher.Status.OK)
			{
				Console.WriteLine("Can't stitch images, error code = {0} ", (int)status);
				return;
			}
			Cv2.NamedWindow(result_name, WindowFlags.Normal);
			Cv2.ImShow(result_name, pano);

		}

	}
}

//c#winform程序中全局变量设置
//https://www.cnblogs.com/jingua1026/articles/1255675.html
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using OpenCvSharp;
using Size  = OpenCvSharp.Size;
using Point = OpenCvSharp.Point;
namespace Cs_Vision
{
	[Serializable]
	public class PolyClass : ISerializable
	{
		public  int region_index;
		public  Rect region_rect;
		public  Point[]? poly_points ;
		public  int edge_id = -1;//手动更新
		public  int poly_type =-1;//
		public  RotatedRect poly_rbox = new RotatedRect();
		public  Point poly_center  = new Point(); 
		private Point edge_pt1     = new Point(); 
		private Point edge_pt2     = new Point(); 
		public  Point gline_pt1    = new Point(); 
		public  Point gline_pt2    = new Point(); 
		public int   edge_distance = 0; 
		public float    edge_angle = 0; 
		public double    poly_area = 0; 
		public double  poly_circum = 0;
		public double   poly_alike = 0;
		public bool     useful = true;
		public static pick_rect pick_irect = new pick_rect();
		public PolyClass()
		{
		}
		public PolyClass(SerializationInfo info, StreamingContext context)
		{
			edge_id = (int)info.GetValue("edge_id", typeof(int));
			poly_type = (int)info.GetValue("poly_type", typeof(int));
			region_index = (int)info.GetValue("region_index", typeof(int));
			region_rect = (Rect)info.GetValue("region_rect", typeof(Rect));
			poly_points = (Point[])info.GetValue("poly_points", typeof(Point[]));
			pick_irect = (pick_rect)info.GetValue("pick_irect", typeof(pick_rect));

			//Deserialize RotatedRect properties
			//Point2f center = (Point2f)info.GetValue("rbox_center", typeof(Point2f));
			//Size2f size = (Size2f)info.GetValue("rbox_size", typeof(Size2f));
			//float angle = (float)info.GetValue("rbox_angle", typeof(float));
			//poly_rbox = new RotatedRect(center, size, angle);
		}
		public  PolyClass(int index, OpenCvSharp.Rect region, OpenCvSharp.Point[] poly)
		{
			this.region_index = index;
			this.region_rect = region;
			this.poly_points = poly;
			this.poly_rbox =  Cv2.MinAreaRect(poly);
			this.poly_center = region.TopLeft + (Point)poly_rbox.Center;
			poly_area      = Cv2.ContourArea(poly_points,true);
			poly_circum    = Cv2.ArcLength(poly_points, true);
		}
		public void PolyUpdate(PolyClass curr)
		{
			//edge_id       = curr.edge_id;
			//poly_type     = curr.poly_type;
			region_index  = curr.region_index;
			region_rect   = curr.region_rect;
			poly_points   = curr.poly_points;
			poly_rbox     = curr.poly_rbox;
            if (poly_type == 1)poly_center = new OpenCvSharp.Point((edge_pt1.X + edge_pt2.X) / 2.0f, (edge_pt1.Y + edge_pt2.Y) / 2.0)+ region_rect.TopLeft;		
			//edge_angle    = curr.edge_angle;
			//edge_distance = curr.edge_distance;
			poly_area     = curr.poly_area;
			poly_circum   = curr.poly_circum;

			if (edge_id != -1 && edge_id < poly_points.Length)
			{
				edge_pt1  = poly_points[edge_id];
				edge_pt2  = poly_points[(edge_id + 1) % poly_points.Length];
				gline_pt1 = region_rect.TopLeft + edge_pt1;
				gline_pt2 = region_rect.TopLeft + edge_pt2;
			}
			if (edge_pt1.Y > edge_pt2.Y)edge_angle = CsTools.Angle2(edge_pt1, edge_pt2, 180);
			else edge_angle = CsTools.Angle2(edge_pt2, edge_pt1, 180);
			edge_distance   = CsTools.Distance(edge_pt1, edge_pt2);

		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
			info.AddValue("edge_id", edge_id);
			info.AddValue("poly_type", poly_type);
			info.AddValue("region_index", region_index);
			info.AddValue("region_rect", region_rect);
			info.AddValue("poly_points", poly_points);
			info.AddValue("pick_irect", pick_irect);

			// Serialize RotatedRect properties
			//info.AddValue("rbox_center", poly_rbox.Center);
			//info.AddValue("rbox_size", poly_rbox.Size);
			//info.AddValue("rbox_angle", poly_rbox.Angle);

		}

		public  static void SaveData(string filename, PolyClass data)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				formatter.Serialize(stream, data);
			}
		}
		public static PolyClass LoadData(string filename)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				object obj = formatter.Deserialize(stream);
				return (PolyClass)obj;
			}
		}
		public static void SaveDictPoly(string filename, List<poly_class> dict_cpoly)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				formatter.Serialize(stream, dict_cpoly);
				//Console.WriteLine("保存dict_cpoly自定义类");
			}
		}
		public static List<poly_class> LoadDictPoly(string filename)
		{
			//Console.WriteLine("加载poly_class对象 ");
			BinaryFormatter formatter = new BinaryFormatter();
			using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				object obj = formatter.Deserialize(stream);
				return (List<poly_class>)obj;
			}
		}
		public static void SaveSomeRects(string filename, List<CsRegion> some_rects)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				formatter.Serialize(stream, some_rects);
				//Console.WriteLine("保存some_rects自定义类");
			}
		}
		public static List<CsRegion> LoadSomeRects(string filename)
		{
			//Console.WriteLine("加载CsRegion对象 ");
			BinaryFormatter formatter = new BinaryFormatter();
			using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				object obj = formatter.Deserialize(stream);
				return (List<CsRegion>)obj;
			}
		}

		public static void DoSomething()
		{
			// 加载PolyClass对象
			PolyClass poly = PolyClass.LoadData("data.bin");

			// 访问PolyClass对象的属性
			Console.WriteLine("Edge ID: "   + poly.edge_id);
			Console.WriteLine("Poly Type: " + poly.poly_type);
			Console.WriteLine("Region Index: " + poly.region_index);
			Console.WriteLine("Region Rect: " + poly.region_rect.ToString());
			Console.WriteLine("Poly Points: ");
			foreach (OpenCvSharp.Point p in poly.poly_points)
			{
				Console.WriteLine(p.ToString());
			}
			//Console.WriteLine("Rotated Rect: " + poly.poly_rbox.Size.ToString());
			// 可以将PolyClass对象传递给函数并使用
		}
	}
}

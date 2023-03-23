//using System;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using OpenCvSharp.Extensions;
using OpenCvSharp;
using System.Collections.Generic;
namespace Cs_Vision
{
	public class CsPram
	{
		private int data;

		private static CsPram instance = new CsPram();
		//private CsPram() { }
		public static CsPram Instance
		{
			get { return instance; }
			set { instance = value; }
		}
		public int Data
		{
			get { return data; }
			set { data = value; }
		}
		public OpenCvSharp.Point last_point;
		public OpenCvSharp.Point curr_point;
		public Rect draw_rect;
		public pick_rect pickregion = new pick_rect();
		public OpenCvSharp.Point pick_point;

		public Mat frame_image = new();
		public Mat result_image = new ();
		public Mat mask_image = new ();
		public Mat hsv_image = new();
		public Bitmap bitmap = new(1,1);

		public Scalar black_color  = new Scalar(0, 0, 0);
		public Scalar white_color  = new Scalar(255, 255, 255);
		public Scalar pink_color   = new Scalar(255, 0, 255);
		public Scalar cyan_color   = new Scalar(255, 255, 0);
		public Scalar red_color    = new Scalar(0, 0, 255);
		public Scalar yellow_color = new Scalar(0, 255, 255);
		public Scalar green_color  = new Scalar(0, 255, 0);
		public Scalar blue_color   = new Scalar(255, 0, 0);
		public Scalar blue_150 = new Scalar(255, 150, 150);
		public Scalar gray_128 = new Scalar(128, 128, 128);
		public Scalar gray_100 = new Scalar(100, 100, 100);
		public Scalar gray_200 = new Scalar(200, 200, 200);
		
		public enum PICKTYPE { 未知形状, 多边形边, 旋转矩形 };

		public List<poly_feture> dict_polys = new List<poly_feture>();
		public List<CsRegion>    some_rects = new List<CsRegion>();

		public bool is_textupdate = false;
		public bool is_moreselect = false;
		public bool is_mouseclick = false;
		public bool is_drawregion = false;
		public bool is_moveregion = false;
		public bool is_dellregion = false;
		public bool is_trackeron  = false;
		public bool is_trackerrun = false;

		public string[] namedatas = new string[9] { "位置 X","距离 D","亮度 L", "位置 Y", "面积 S", "数量 N", "角度 A", "宽度 W", "高度 H" };
		public int izoomVal = 3;
		public int thrldVal = 50;
		public int areasMin = 18;
		public int areasMax = 90;
		public int alike60  = 60;
		public int asideVal = 21;
		public int AREA3600 = 3600;
		public int  run_mode = 0;
		public bool run_loop = true;
		public bool run_reset  = false;

		public VideoCapture videocap = new VideoCapture(0);

		public CsDatas? current_csdata;
		public Mat cam_coodinate_mat = new Mat(9, 2, MatType.CV_64F, new Scalar(0));
		public Mat rot_coodinate_mat = new Mat(9, 2, MatType.CV_64F, new Scalar(0));
	}
	public class pick_rect
	{
		public int index = -1;
		public int type = -1;
		public Scalar ok_ng = new Scalar(255,255,0);
		public Rect region = new Rect();
		public pick_rect()
		{
		}
	}
	public class poly_feture
	{
		public int index = 0 ;
		public Rect rect = new Rect();
		public PolyFeature feat = new PolyFeature();
		public poly_feture(PolyFeature ft)
		{
			index = ft.region_index;
			rect  = ft.region_rect;
			feat  = ft;
		}
	}

	public class CsRegion
	{
		public int type;
		public int index;
		public int value;
		public Scalar ok_ng = Scalar.Gray;
		public Rect rect;
		public Vec3b color;
		public AsDll.AsDlib tracker = new AsDll.AsDlib();
		public CsDatas csdata = new CsDatas();
		public float[] mindatas = new float[9];
		public float[] maxdatas = new float[9];

		public CsRegion(Rect cs_rect, Vec3b cs_color, int cs_value,int cs_tpye,int cs_index)
		{
			rect = cs_rect;
			color = cs_color;
			value = cs_value;
			type = cs_tpye;
			index = cs_index;
            if (cs_tpye == 1)tracker = new AsDll.AsDlib();
		}
	}

}

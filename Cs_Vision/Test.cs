
using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Cs_Vision
{
    public class Test
    {
        static List<OpenCvSharp.Point[]> polygons;

        public static void MM()
        {
            // 加载图像
            Mat image = Cv2.ImRead(@"D:\TestApp\Cs_Vision\Cs_Vision\bin\Debug\net6.0-windows\test1.jpg");

            // 灰度化
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            // 二值化
            Mat thresh = new Mat();
            Cv2.Threshold(gray, thresh, 200, 255, ThresholdTypes.Binary);

            // 寻找轮廓
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // 处理轮廓
            polygons = new List<OpenCvSharp.Point[]>();
            foreach (OpenCvSharp.Point[] contour in contours)
            {
                double epsilon = 0.01 * Cv2.ArcLength(contour, true);
                OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(contour, epsilon, true);
                polygons.Add(approx);
            }

            // 创建窗口并注册鼠标事件
            Cv2.NamedWindow("image");
            Cv2.SetMouseCallback("image", (event_, x, y, flags, userdata) =>
            {
                if (event_ == MouseEventTypes.LButtonDown)
                {
                    OpenCvSharp.Point p = new OpenCvSharp.Point(x, y);
                    OpenCvSharp.Point[] nearestPolygon = null;
                    OpenCvSharp.Point[] nearestEdge = null;
                    double nearestPolygonDistance = double.MaxValue;
                    double nearestEdgeDistance = double.MaxValue;

                    foreach (OpenCvSharp.Point[] polygon in polygons)
                    {
                        foreach (OpenCvSharp.Point point in polygon)
                        {
                            double distance = Cv2.PointPolygonTest(polygon, p, true);
                            if (distance < nearestPolygonDistance)
                            {
                                nearestPolygon = polygon;
                                nearestPolygonDistance = distance;
                            }
                            if (distance < nearestEdgeDistance && distance >= 0)
                            {
                                int index = Array.IndexOf(polygon, point);
                                OpenCvSharp.Point[] edge = new OpenCvSharp.Point[] { polygon[index], polygon[(index + 1) % polygon.Length] };
                                nearestEdge = edge;
                                nearestEdgeDistance = distance;
                            }
                        }
                    }

                    if (nearestEdge != null)
                    {
                        Cv2.Line(image, nearestEdge[0], nearestEdge[1], Scalar.Green, 5);
                    }

                    Cv2.ImShow("image", image);
                    Cv2.WaitKey(1);
                }
            });

            Cv2.ImShow("image", image);
            Cv2.WaitKey(0);
        }
    }
}








/*/





using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Cs_Vision
{
    public class PProgram
    {
        static List<OpenCvSharp.Point[]> polygons;
        static bool shiftPressed = false;
        static OpenCvSharp.Point[] lastEdge = null;





        public static void MMain()
        {

            // 加载图像
             Mat image = Cv2.ImRead(@"D:\TestApp\Cs_Vision\Cs_Vision\bin\Debug\net6.0-windows\test1.jpg");

            // 灰度化
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            // 二值化
            Mat thresh = new Mat();
            Cv2.Threshold(gray, thresh, 200, 255, ThresholdTypes.Binary);

            // 寻找轮廓
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // 处理轮廓
            polygons = new List<OpenCvSharp.Point[]>();
            foreach (OpenCvSharp.Point[] contour in contours)
            {
                double epsilon = 0.01 * Cv2.ArcLength(contour, true);
                OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(contour, epsilon, true);
                polygons.Add(approx);
            }

            // 创建窗口并注册鼠标事件
            Cv2.NamedWindow("image");
            Cv2.SetMouseCallback("image", (event_, x, y, flags, userdata) =>
            {
                // 鼠标左键按下时，查找最近的多边形和边
                if (event_ == MouseEventTypes.LButtonDown)
                {
                    OpenCvSharp.Point p = new OpenCvSharp.Point(x, y);
                    OpenCvSharp.Point[] nearestPolygon = null;
                    OpenCvSharp.Point[] nearestEdge = null;
                    double nearestPolygonDistance = double.MaxValue;
                    double nearestEdgeDistance = double.MaxValue;

                    foreach (OpenCvSharp.Point[] polygon in polygons)
                    {
                        foreach (OpenCvSharp.Point point in polygon)
                        {
                            double distance = Cv2.PointPolygonTest(polygon, p, true);
                            if (distance < nearestPolygonDistance)
                            {
                                nearestPolygon = polygon;
                                nearestPolygonDistance = distance;
                            }
                            if (distance < nearestEdgeDistance && distance >= 0)
                            {
                                int index = Array.IndexOf(polygon, point);
                                OpenCvSharp.Point[] edge = new OpenCvSharp.Point[] { polygon[index], polygon[(index + 1) % polygon.Length] };
                                nearestEdge = edge;
                                nearestEdgeDistance = distance;
                            }
                        }
                    }

                    // 如果找到了多边形和边，则在图像上标记它们
                   // if (nearestPolygon != null)
                   // {
                   //     Cv2.Polylines(image, new OpenCvSharp.Point[][] { nearestPolygon }, true, Scalar.Red, 2);
                   // }
                    if (nearestEdge != null)
                    {
                        if (shiftPressed || lastEdge != null)
                        {
                            Cv2.Line(image, lastEdge[1], nearestEdge[0], Scalar.Green, 5);
                        }
                        Cv2.Line(image, nearestEdge[0], nearestEdge[1], Scalar.Green, 5);
                        lastEdge = nearestEdge;
                    }
                    else
                    {
                        lastEdge = null;
                    }

                    // 更新窗口显示
                    Cv2.ImShow("image", image);
                    Cv2.WaitKey(1);
                }
            });


            // 注册键盘事件
         





        }
    }
}

//*/
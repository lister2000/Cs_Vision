using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using OpenCvSharp;
using System.Runtime.InteropServices;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;
namespace Cs_Vision
{

    [Serializable]
    public class CustomData 
    {
        public Rect RectData { get; set; }
        public Point PointData { get; set; }
        public Point[] PointArrayData { get; set; }
        public List<Rect> RectListData { get; set; }
        public List<Mat> MatListData { get; set; }
        public  RotatedRect PolyRbox { get; set; }
        public CustomData()
        {
            RectData = new Rect();
            PointData = new Point();
            PointArrayData = new Point[0];
            RectListData = new List<Rect>();
            MatListData = new List<Mat>();
            PolyRbox = new RotatedRect();
        }

        private static byte[] ConvertMatToArray(Mat mat)
        {
            int dataSize = mat.Rows * mat.Cols * mat.ElemSize();
            byte[] data = new byte[dataSize];
            if (mat.IsContinuous())
            {
                Marshal.Copy(mat.Data, data, 0, dataSize);
            }
            else
            {
                int pos = 0;
                for (int i = 0; i < mat.Rows; i++)
                {
                    IntPtr ptr = mat.Ptr(i);
                    Marshal.Copy(ptr, data, pos, mat.Cols * mat.ElemSize());
                    pos += mat.Cols * mat.ElemSize();
                }
            }
            return data;
        }
        // Serialize the CustomData object to a binary file
        private   void Serialize(string fileName)
        {
            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // formatter.Serialize(stream, this);

                formatter.Serialize(stream, RectData);
                formatter.Serialize(stream, PointData);
                formatter.Serialize(stream, PointArrayData);
                formatter.Serialize(stream, RectListData);
                formatter.Serialize(stream, MatListData.Count);
                foreach (Mat mat in MatListData)
                {
                    int rows = mat.Rows;
                    int cols = mat.Cols;
                    int type = (int)mat.Type();
                    byte[] data = ConvertMatToArray(mat); 
                    formatter.Serialize(stream, rows);
                    formatter.Serialize(stream, cols);
                    formatter.Serialize(stream, type);
                    formatter.Serialize(stream, data);
                }
                Point2f position = PolyRbox.Center;
                Size2f size = PolyRbox.Size;
                float angle = PolyRbox.Angle;
                formatter.Serialize(stream, position);
                formatter.Serialize(stream, size);
                formatter.Serialize(stream, angle);


            }
        }

        // Deserialize the CustomData object from a binary file
         static CustomData Deserialize(string fileName)
        {
            CustomData data = new CustomData();
            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                data.RectData = (Rect)formatter.Deserialize(stream);
                data.PointData = (Point)formatter.Deserialize(stream);
                data.PointArrayData = (Point[])formatter.Deserialize(stream);
                data.RectListData = (List<Rect>)formatter.Deserialize(stream);
                int matListCount = (int)formatter.Deserialize(stream);
                for (int i = 0; i < matListCount; i++)
                {
                    int rows = (int)formatter.Deserialize(stream);
                    int cols = (int)formatter.Deserialize(stream);
                    MatType type = (MatType)(int)formatter.Deserialize(stream);
                    byte[] dataBytes = (byte[])formatter.Deserialize(stream);
                    Mat mat = new Mat(rows, cols, type, dataBytes);
                    data.MatListData.Add(mat);
                }

                Point2f position = (Point2f)formatter.Deserialize(stream);
                Size2f size = (Size2f)formatter.Deserialize(stream);
                float angle = (float)formatter.Deserialize(stream);
                data.PolyRbox = new RotatedRect(position,size, angle);
            }
            return data;
        }


        public static void SaveToFile()

        {
            CustomData data = new CustomData();
            data.RectData = new Rect(10, 0, 300, 100);
            data.PointData = new Point(350, 350);
            data.PointArrayData = new Point[] { new Point(10, 10), new Point(20, 20), new Point(20, 30) };
            data.RectListData = new List<Rect>() { new Rect(0, 0, 50, 50), new Rect(50, 50, 50, 50) };
            data.PolyRbox = new RotatedRect(new Point2f(11, 22), new Size2f(2200, 300), 66.6f); 



            data.MatListData = new List<Mat>() { new Mat(100, 100, MatType.CV_8UC3, new Scalar(0, 0, 255)),
                                     new Mat(200, 200, MatType.CV_8UC1, new Scalar(255)),
                                     new Mat(300, 300, MatType.CV_32FC1, new Scalar(1.23f)) };

            data.Serialize("custom_data.bin");

        }

        public static  void  ReadFromFile()
        {

              CustomData data = CustomData.Deserialize("custom_data.bin");
            //  Console.WriteLine("RectData: " + data.RectData.ToString());
            //  Console.WriteLine("PointData: " + data.PointData.ToString());
            //  Console.WriteLine("PointArrayData:");
            //  foreach (Point p in data.PointArrayData)
            //  {
            //      Console.WriteLine("\t" + p.ToString());
            //  }
            //  Console.WriteLine("RectListData:");
            //  foreach (Rect r in data.RectListData)
            //  {
            //      Console.WriteLine("\t" + r.ToString());
            //  }
            //
            //  Console.WriteLine("MatListData:");
            //  foreach (Mat m in data.MatListData)
            //  {
            //      Console.WriteLine("\t" + m.ToString());
            //  }

            Console.WriteLine("RectData: " + data.PolyRbox.Size.ToString());

        }
    }

}


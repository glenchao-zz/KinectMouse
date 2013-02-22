using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace VirtualMouse
{
    class SurfaceDetection
    {
        //private Array Surface(bitmap, skeletonframe)
        //{
            
        
        //}
        public DepthImagePixel[] emptyFrame { get; set; }
        public DepthImagePixel[] playerFrame { get; set; }
        public Point jointPoint { get; set; }
        public Plane surface { get; set; }
        private int distance = 2;

        public SurfaceDetection() { }
        
        public Plane getSurface()
        {
            int index = Helper.Point2DepthIndex(jointPoint);
            short depth = emptyFrame[index].Depth;
            Vector origin = new Vector(jointPoint.X, jointPoint.Y, (double)depth);

            Point point1 = new Point(jointPoint.X - distance, jointPoint.Y);
            int index1 = Helper.Point2DepthIndex(point1);
            short depth1 = emptyFrame[index1].Depth;
            Vector vector1 = new Vector(point1.X, point1.Y, (double)depth1);

            Point point2 = new Point(jointPoint.X, jointPoint.Y - distance);
            int index2 = Helper.Point2DepthIndex(point2);
            short depth2 = emptyFrame[index2].Depth;
            Vector vector2 = new Vector(point2.X, point2.Y, (double)depth2);

            Vector vectorA = origin.Subtraction(vector1);
            Vector vectorB = origin.Subtraction(vector2);

            Vector normal = vectorA.CrossProduct(vectorB);
            this.surface = new Plane(normal, origin);
            return this.surface;
        }
    }

    class Vector
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public Vector(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector Subtraction(Vector v)
        {
            return new Vector(this.x - v.x, this.y - v.y, this.z - v.z);
        }

        public Vector CrossProduct(Vector v)
        {
            return new Vector(this.y * v.z - v.y * this.z,
                              this.z * v.x - this.x * v.z,
                              this.x * v.y - v.x * this.y);
        }

        public double DotProduct(Vector v)
        {
            return this.x * v.x + this.y * v.y + this.z * v.z;
        }

        public Vector Normalize()
        {
            double mag = Math.Sqrt(this.x * this.x + this.y * this.y + this.z * this.z);
            return new Vector(this.x / mag, this.y / mag, this.z / mag);
        }
    }

    class Plane
    {
        public Vector normal { get; set; }
        public double d { get; set; }
        
        public Plane(Vector norm, double d)
        {
            this.normal = norm.Normalize();
            this.d = d;
        }

        public Plane(Vector normal, Vector v)
        {
            this.normal = normal;
            this.d = normal.DotProduct(v);
        }

        public double IsOnPlane(Vector v)
        {
            double diff = Math.Abs(this.normal.DotProduct(v) - this.d);;
            //return diff < 1000 ? true : false;
            return diff;
        }
    }
}

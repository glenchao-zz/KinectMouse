using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace VirtualMouse
{
    internal class SurfaceDetection
    {
        //private Array Surface(bitmap, skeletonframe)
        //{


        //}
        public DepthImagePixel[] emptyFrame { get; set; }
        public int[] surfaceMatrix { get; set; }
        public Point definitionPoint { get; set; }
        public Vector origin { get; set; }
        public Vector sample1 { get; set; }
        public Vector sample2 { get; set; }
        public Vector vectorA { get; set; }
        public Vector vectorB { get; set; }
        public Plane surface { get; set; }
        private int distance = 2;

        public SurfaceDetection()
        {
        }

        public Plane getSurface()
        {
            int index = Helper.Point2DepthIndex(definitionPoint);
            short depth = emptyFrame[index].Depth;
            this.origin = new Vector(definitionPoint.X, definitionPoint.Y, (double)depth);

            Point point1 = new Point(definitionPoint.X - distance, definitionPoint.Y);
            int index1 = Helper.Point2DepthIndex(point1);
            short depth1 = emptyFrame[index1].Depth;
            this.sample1 = new Vector(point1.X, point1.Y, (double) depth);

            Point point2 = new Point(definitionPoint.X, definitionPoint.Y - distance);
            int index2 = Helper.Point2DepthIndex(point2);
            short depth2 = emptyFrame[index2].Depth;
            this.sample2 = new Vector(point2.X, point2.Y, (double) depth2);

            this.vectorA = this.sample1.Subtraction(this.origin);
            this.vectorB = this.sample2.Subtraction(this.origin);

            Vector normal = vectorA.CrossProduct(vectorB);
            this.surface = new Plane(normal, origin);

            getSurfaceMatrix();

            return this.surface;
        }

        public int[] getSurfaceMatrix()
        {
            this.surfaceMatrix = new int[emptyFrame.Length];
            for (int i = 0; i < this.emptyFrame.Length; ++i)
            {
                //find x,y,z cordinates 
                double x = i%640;
                double y = (i - x)/640;
                double z = (double) emptyFrame[i].Depth;

                double diff = this.surface.DistanceToPoint(x, y, z);
                if (diff < 10)
                {
                    this.surfaceMatrix[i] = (byte) diff;
                }
                else
                {
                    this.surfaceMatrix[i] = -1;
                }
            }
            return this.surfaceMatrix;
        }
    }

    internal class Vector
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
            return new Vector(this.y*v.z - this.z*v.y,
                              this.z*v.x - this.x*v.z,
                              this.x*v.y - this.y*v.x);
        }

        public double DotProduct(double x, double y, double z)
        {
            return this.x*x + this.y*y + this.z*z;
        }

        public double DotProduct(Vector v)
        {
            return this.DotProduct(v.x, v.y, v.z);
        }

        public Vector Normalize()
        {
            double mag = Math.Sqrt(this.x*this.x + this.y*this.y + this.z*this.z);
            return new Vector(this.x/mag, this.y/mag, this.z/mag);
        }

        public override string ToString()
        {
            double x = Math.Round(this.x, 2);
            double y = Math.Round(this.y, 2);
            double z = Math.Round(this.z, 2);
            return String.Format("x: {0}, y: {1}, z: {2}", x, y, z);
        }
    }

    internal class Plane
    {
        public Vector normal { get; set; }
        public double d { get; set; }

        public Plane(Vector normal, double d)
        {
            this.normal = normal.Normalize();
            this.d = d;
        }

        public Plane(Vector normal, Vector v)
        {
            this.normal = normal.Normalize();
            this.d = this.normal.DotProduct(v);
        }

        public double DistanceToPoint(double x, double y, double z)
        {
            return Math.Abs(this.normal.DotProduct(x, y, z) - this.d);
        }

        public double DistanceToPoint(Vector v)
        {
            return this.DistanceToPoint(v.x, v.y, v.z);
        }

        public override string ToString()
        {
            double d = Math.Round(this.d, 2);
            return String.Format(this.normal.ToString() + " D: {0}", d);
        }
    }
}

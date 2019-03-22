using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace K_PathFinder.PFDebuger {
    //shader stuff
    public struct PointData {
        public Vector3 pos;
        public Color color;
        public float size;

        public PointData(Vector3 Pos, Color Color, float Size) {
            pos = Pos;
            color = Color;
            size = Size;
        }
    }
    public struct LineData {
        public Vector3 a;
        public Vector3 b;
        public Color color;
        public float width;

        public LineData(Vector3 A, Vector3 B, Color Color, float Width) {
            a = A;
            b = B;
            color = Color;
            width = Width;
        }
    }
    public struct TriangleData {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Color color;

        public TriangleData(Vector3 A, Vector3 B, Vector3 C, Color Color) {
            a = A;
            b = B;
            c = C;
            color = Color;
        }
    }

    //some cool stuff for experiments
    public struct CoolTriangleData {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public CoolTriangleData(Vector3 A, Vector3 B, Vector3 C) {
            a = A;
            b = B;
            c = C;
        }
    }
}
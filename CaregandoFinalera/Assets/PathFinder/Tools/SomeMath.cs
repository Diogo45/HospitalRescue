using UnityEngine;
using System.Collections;
using K_PathFinder.VectorInt ;
using System.Collections.Generic;
using System;
using System.Linq;

//TODO: TLP_Projec_DO_ME
namespace K_PathFinder {
    /// <summary>
    /// junkyard of math
    /// </summary>
    public static class SomeMath {
        public static int SqrDistance(int x1, int y1, int x2, int y2) {
            return ((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1));
        }
        public static float SqrDistance(float x1, float y1, float x2, float y2) {
            return ((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1));
        }
        public static float SqrDistance(float x1, float y1, float z1, float x2, float y2, float z2) {
            return ((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)) + ((z2 - z1) * (z2 - z1));
        }
        public static float SqrDistance(Vector3 v1, Vector3 v2) {
            return SqrDistance(v1.x, v1.y, v1.z, v2.x, v2.y, v2.z);
        }
        public static float SqrDistance(Vector2 v1, Vector2 v2) {
            return SqrDistance(v1.x, v1.y, v2.x, v2.y);
        }

        public static float Min(float a, float b, float c) {
            a = a < b ? a : b;
            return a < c ? a : c;
        }
        public static float Max(float a, float b, float c) {
            a = a > b ? a : b;
            return a > c ? a : c;
        }

        public static int Min(int a, int b, int c) {
            a = a < b ? a : b;
            return a < c ? a : c;
        }
        public static int Max(int a, int b, int c) {
            a = a > b ? a : b;
            return a > c ? a : c;
        }

        public static bool InRangeExclusive(int value, int min, int max) {
            return value > min && value < max; ;
        }
        public static bool InRangeExclusive(float value, float min, float max) {
            return value > min && value < max; ;
        }
        public static bool InRangeInclusive(int value, int min, int max) {
            return value >= min && value <= max; ;
        }
        public static bool InRangeInclusive(float value, float min, float max) {
            return value >= min && value <= max;;
        }


        public static Vector3 TwoVertexNormal(Vector3 first, Vector3 second) {
            return (first.z * second.x) - (first.x * second.z) < 0 ?
                (first.normalized + second.normalized).normalized * -1 :
                (first.normalized + second.normalized).normalized;
        }

        public static float LinePointSideMathf(Vector2 a, Vector2 b, Vector2 point) {
            return Mathf.Sign((point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y));
        }
        public static float LinePointSideMath(Vector2 a, Vector2 b, Vector2 point) {
            return Math.Sign((point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y));
        }
        public static float LinePointSideMath(Vector2 a, Vector2 b, float pointX, float pointY) {
            return Math.Sign((pointX - b.x) * (a.y - b.y) - (a.x - b.x) * (pointY - b.y));
        }

        public static bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 po) {
            float s = a.y * c.x - a.x * c.y + (c.y - a.y) * po.x + (a.x - c.x) * po.y;
            float t = a.x * b.y - a.y * b.x + (a.y - b.y) * po.x + (b.x - a.x) * po.y;

            if ((s <= 0) != (t <= 0))
                return false;

            float A = -b.y * c.x + a.y * (c.x - b.x) + a.x * (b.y - c.y) + b.x * c.y;
            if (A < 0.0) {
                s = -s;
                t = -t;
                A = -A;
            }
            return s > 0 && t > 0 && (s + t) < A;
        }

        //- is right 
        //+ is left
        public static float LineSide(Vector2 A, Vector2 B, Vector2 P) {
            return (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);
        }
        //- is right 
        //+ is left
        public static float LineSide(float Ax, float Ay, float Bx, float By, float pointX, float pointY) {
            return (Bx - Ax) * (pointY - Ay) - (By - Ay) * (pointX - Ax);
        }

        public static bool PointInTriangleSimple(Vector3 A, Vector3 B, Vector3 C, float pointX, float pointZ) {//and little over
            return 
                (LineSide(A.x, A.z, B.x, B.z, pointX, pointZ) <= 0) == 
                (LineSide(B.x, B.z, C.x, C.z, pointX, pointZ) <= 0) == 
                (LineSide(C.x, C.z, A.x, A.z, pointX, pointZ) <= 0);
        }

        public static float CalculateHeight(Vector3 A, Vector3 B, Vector3 C, float x, float z) {
            float det = (B.z - C.z) * (A.x - C.x) + (C.x - B.x) * (A.z - C.z);

            float l1 = ((B.z - C.z) * (x - C.x) + (C.x - B.x) * (z - C.z)) / det;
            float l2 = ((C.z - A.z) * (x - C.x) + (A.x - C.x) * (z - C.z)) / det;
            float l3 = 1.0f - l1 - l2;

            return l1 * A.y + l2 * B.y + l3 * C.y;
        }

        public static List<Vector3> RasterizeTriangleShity(Vector3 A, Vector3 B, Vector3 C, float step) {
            List<Vector3> result = new List<Vector3>();
            for (int x = Mathf.RoundToInt(Math.Min(Math.Min(A.x, B.x), C.x) / step); x < Mathf.RoundToInt(Math.Max(Math.Max(A.x, B.x), C.x) / step); x++) {
                for (int z = Mathf.RoundToInt(Math.Min(Math.Min(A.z, B.z), C.z) / step); z < Mathf.RoundToInt(Math.Max(Math.Max(A.z, B.z), C.z) / step); z++) {
                    if (PointInTriangle(toV2(A), toV2(B), toV2(C), new Vector2(x * step, z * step)))
                        result.Add(new Vector3(x * step, CalculateHeight(A, B, C, x * step, z * step), z * step));
                }
            }
            return result;
        }

        private static Vector2 toV2(Vector3 pos) {
            return new Vector2(pos.x, pos.z);
        }
                
        public static Vector3 NearestPointOnLine(Vector3 linePointA, Vector3 linePointB, Vector3 point) {
            linePointB = linePointA - linePointB;
            linePointB.Normalize();//this needs to be a unit vector
            var v = point - linePointA;
            var d = Vector3.Dot(v, linePointB);
            return linePointA + linePointB * d;
        }

        public static Vector3 NearestPointOnSegment(Vector3 a, Vector3 b, Vector3 point) {
            Vector3 vectorAB = b - a;
            Vector3 vectorAB_normalized = vectorAB.normalized;
            float dot = Vector3.Dot(point - a, vectorAB_normalized);
            Vector3 AB_n_mul_t = vectorAB_normalized * dot;
            Vector3 result = a + AB_n_mul_t;

            if (dot < 0)
                result = a;
            else if (vectorAB.magnitude < AB_n_mul_t.magnitude)
                result = b;
            return result;
        }
        public static bool NearestPointOnSegment(Vector3 a, Vector3 b, Vector3 point, bool clamp, out Vector3 intersection) {
            Vector3 vectorAB = b - a;
            Vector3 vectorAB_normalized = vectorAB.normalized;
            float dot = Vector3.Dot(point - a, vectorAB_normalized);
            Vector3 AB_n_mul_t = vectorAB_normalized * dot;
            intersection = a + AB_n_mul_t;

            if (clamp) {
                if (dot < 0)
                    intersection = a;
                else if (vectorAB.magnitude < AB_n_mul_t.magnitude)
                    intersection = b;
                return true;
            }
            else
                return (dot < 0 || vectorAB.magnitude < AB_n_mul_t.magnitude) == false;
        }

        public static Vector3 NearestPointOnSegmentShitVersion(Vector3 linePointA, Vector3 linePointB, Vector3 pnt, float maxError) {
            Vector3 tempB = linePointA - linePointB;
            tempB.Normalize();//this needs to be a unit vector
            var v = pnt - linePointA;
            var d = Vector3.Dot(v, tempB);
            Vector3 intersection = linePointA + tempB * d;
            Vector3 result;

            float distanceAB = Vector3.Distance(linePointA, linePointB);
            float distancePA = Vector3.Distance(intersection, linePointA);
            float distancePB = Vector3.Distance(intersection, linePointB);

            if (Math.Abs((distancePA + distancePB) - distanceAB) < maxError)
                result = intersection;
            else {
                if (distancePA < distancePB)
                    result = linePointA;
                else
                    result = linePointB;
            }
            return result;
        }

        public static float TriangleArea(Vector2 a, Vector2 b, Vector2 c) {
            return Math.Abs(Vector3.Cross(b - a, c - a).z) * 0.5f;
        }

        #region not-a-k_math-still-good
        //public static Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point) {

        //    Vector3 vector = linePoint2 - linePoint1;

        //    Vector3 projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

        //    int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);
        //    Debug.Log(side);

        //    //The projected point is on the line segment
        //    if (side == 0) {

        //        return projectedPoint;
        //    }

        //    if (side == 1) {

        //        return linePoint1;
        //    }

        //    if (side == 2) {

        //        return linePoint2;
        //    }

        //    //output is invalid
        //    return Vector3.zero;
        //}
        //public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point) {

        //    //get vector from point on line to point in space
        //    Vector3 linePointToPoint = point - linePoint;

        //    float t = Vector3.Dot(linePointToPoint, lineVec);

        //    return linePoint + lineVec * t;
        //}
        //public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point) {

        //    Vector3 lineVec = linePoint2 - linePoint1;
        //    Vector3 pointVec = point - linePoint1;

        //    float dot = Vector3.Dot(pointVec, lineVec);

        //    //point is on side of linePoint2, compared to linePoint1
        //    if (dot > 0) {

        //        //point is on the line segment
        //        if (pointVec.magnitude <= lineVec.magnitude) {

        //            return 0;
        //        }

        //        //point is not on the line segment and it is on the side of linePoint2
        //        else {

        //            return 2;
        //        }
        //    }

        //    //Point is not on side of linePoint2, compared to linePoint1.
        //    //Point is not on the line segment and it is on the side of linePoint1.
        //    else {

        //        return 1;
        //    }
        //}
        #endregion

        public static Vector2 NearestPointOnLine(Vector2 linePointA, Vector2 linePointB, Vector2 point) {
            linePointB = linePointA - linePointB;
            linePointB.Normalize();//this needs to be a unit vector
            var v = point - linePointA;
            var d = Vector2.Dot(v, linePointB);
            return linePointA + linePointB * d;
        }

        public static Vector3 MidPoint(params Vector3[] input) {
            Vector3 output = Vector3.zero;
            foreach (var item in input)
                output += item;

            return output / input.Length;
        }        
        public static Vector3 MidPoint(IEnumerable<Vector3> input) {
            Vector3 output = Vector3.zero;
            foreach (var item in input)
                output += item;

            return output / input.Count();
        }
        public static Vector2 MidPoint(params Vector2[] input) {
            Vector2 output = Vector2.zero;
            foreach (var item in input)
                output += item;

            return output / input.Length;
        }
        public static Vector2 MidPoint(IEnumerable<Vector2> input) {
            Vector2 output = Vector2.zero;
            foreach (var item in input)
                output += item;

            return output / input.Count();
        }

        public static List<Vector2> DouglasPeucker(List<Vector2> points, int startIndex, int lastIndex, float epsilon) {
            float dmax = 0f;
            int index = startIndex;

            for (int i = index + 1; i < lastIndex; ++i) {
                float d = PointLineDistance(points[i], points[startIndex], points[lastIndex]);
                if (d > dmax) {
                    index = i;
                    dmax = d;
                }
            }

            if (dmax > epsilon) {
                var res1 = DouglasPeucker(points, startIndex, index, epsilon);
                var res2 = DouglasPeucker(points, index, lastIndex, epsilon);

                var finalRes = new List<Vector2>();
                for (int i = 0; i < res1.Count - 1; ++i) {
                    finalRes.Add(res1[i]);
                }

                for (int i = 0; i < res2.Count; ++i) {
                    finalRes.Add(res2[i]);
                }

                return finalRes;
            }
            else {
                return new List<Vector2>(new Vector2[] { points[startIndex], points[lastIndex] });
            }
        }

        public static float PointLineDistance(Vector2 point, Vector2 start, Vector2 end) {
            if (start == end) {
                return Vector2.Distance(point, start);
            }

            float n = Mathf.Abs((end.x - start.x) * (start.y - point.y) - (start.x - point.x) * (end.y - start.y));
            float d = Mathf.Sqrt((end.x - start.x) * (end.x - start.x) + (end.y - start.y) * (end.y - start.y));

            return n / d;
        }

        public static List<Vector3> DouglasPeucker(List<Vector3> points, int startIndex, int lastIndex, float epsilon) {
            float dmax = 0f;
            int index = startIndex;

            for (int i = index + 1; i < lastIndex; ++i) {
                float d = Vector3.Distance(NearestPointOnLine(points[startIndex], points[lastIndex], points[i]), points[i]);
                if (d > dmax) {
                    index = i;
                    dmax = d;
                }
            }

            if (dmax > epsilon) {
                var res1 = DouglasPeucker(points, startIndex, index, epsilon);
                var res2 = DouglasPeucker(points, index, lastIndex, epsilon);

                var finalRes = new List<Vector3>();
                for (int i = 0; i < res1.Count - 1; ++i) {
                    finalRes.Add(res1[i]);
                }

                for (int i = 0; i < res2.Count; ++i) {
                    finalRes.Add(res2[i]);
                }

                return finalRes;
            }
            else {
                return new List<Vector3>(new Vector3[] { points[startIndex], points[lastIndex] });
            }
        }

        public static float V2Cross(Vector2 left, Vector2 right) {
            return (left.y * right.x) - (left.x * right.y);
        }
        public static float V2Cross(float leftX, float leftY, float rightX, float rightY) {
            return (leftY * rightX) - (leftX * rightY);
        }

        #region projection
        public static Vector3 ClosestToLineTopProjection(Vector3 lineA, Vector3 lineB, Vector2 point) {
            Vector3 pointV3 = new Vector3(point.x, 0, point.y);
            Vector3 lineVec1 = lineB - lineA;
            Vector3 lineVec2 = Vector3.down;

            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = a * e - b * b;

            if (d == 0f)
                Debug.LogError("Lines are paralel");

            Vector3 r = lineA - pointV3;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);
            float s = (b * f - c * e) / d;

            return lineA + lineVec1 * s;
        }

        public static bool ClosestToSegmentTopProjection(Vector3 lineA, Vector3 lineB, Vector2 point, out Vector3 intersection) {
            Vector3 pointV3 = new Vector3(point.x, 0, point.y);
            Vector3 lineVec1 = lineB - lineA;
            Vector3 lineVec2 = Vector3.down;

            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = a * e - b * b;

            if (d == 0f) {
                intersection = Vector3.zero;
                return false;
            }

            Vector3 r = lineA - pointV3;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);
            float s = (b * f - c * e) / d;

            intersection = lineA + lineVec1 * s;
            return s >= 0 & s <= 1f;
        }

        public static bool ClosestToSegmentTopProjection(Vector3 lineA, Vector3 lineB, Vector2 point, bool clamp, out Vector3 intersection) {
            Vector3 pointV3 = new Vector3(point.x, 0, point.y);
            Vector3 lineVec1 = lineB - lineA;
            Vector3 lineVec2 = Vector3.down;

            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = a * e - b * b;

            if (d == 0f) {
                intersection = Vector3.zero;
                return false;
            }

            Vector3 r = lineA - pointV3;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);
            float s = (b * f - c * e) / d;


            if (clamp) {
                s = Mathf.Clamp01(s);
                intersection = lineA + lineVec1 * s;
                return true;
            }
            else {
                intersection = lineA + lineVec1 * s;
                return s >= 0 & s <= 1f;
            }
        }

        public static bool TwoLinesProjectionByX(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, float maxDist, out Vector2 minus, out Vector2 plus) {
            minus = Vector2.zero;
            plus = Vector2.zero;

            if (a1.x == a2.x || b1.x == b2.x)//cause nono
                return false;

            //1.x < 2.x
            if (a1.x > a2.x) {
                Vector2 temp = a2;
                a2 = a1;
                a1 = temp;
            }

            if (b1.x > b2.x) {
                Vector2 temp = b2;
                b2 = b1;
                b1 = temp;
            }

            //if we dont overlap by X then no-no
            if ((TLP_InRange(a1.x, a2.x, b1.x) || TLP_InRange(a1.x, a2.x, b2.x) || TLP_InRange(b1.x, b2.x, a1.x) || TLP_InRange(b1.x, b2.x, a2.x)) == false)
                return false;


            Vector2? resultMinus = null, resultPlus = null;

            Vector2 point;
            if (TLP_InRange(a1.x, a2.x, b1.x) && TLP_Project(a1, a2, b1, maxDist, out point)) {
                if (resultMinus == null) {
                    resultMinus = point;
                    resultPlus = point;
                }
                else {
                    if (point.x < resultMinus.Value.x)
                        resultMinus = point;

                    if (point.x > resultPlus.Value.x)
                        resultPlus = point;
                }
            }

            if (TLP_InRange(a1.x, a2.x, b2.x) && TLP_Project(a1, a2, b2, maxDist, out point)) {
                if (resultMinus == null) {
                    resultMinus = point;
                    resultPlus = point;
                }
                else {
                    if (point.x < resultMinus.Value.x)
                        resultMinus = point;

                    if (point.x > resultPlus.Value.x)
                        resultPlus = point;
                }
            }

            if (TLP_InRange(b1.x, b2.x, a1.x) && TLP_Project(b1, b2, a1, maxDist, out point)) {
                if (resultMinus == null) {
                    resultMinus = point;
                    resultPlus = point;
                }
                else {
                    if (point.x < resultMinus.Value.x)
                        resultMinus = point;

                    if (point.x > resultPlus.Value.x)
                        resultPlus = point;
                }
            }

            if (TLP_InRange(b1.x, b2.x, a2.x) && TLP_Project(b1, b2, a2, maxDist, out point)) {
                if (resultMinus == null) {
                    resultMinus = point;
                    resultPlus = point;
                }
                else {
                    if (point.x < resultMinus.Value.x)
                        resultMinus = point;

                    if (point.x > resultPlus.Value.x)
                        resultPlus = point;
                }
            }

            if (resultMinus != null && resultMinus.Value != resultPlus.Value) {
                minus = resultMinus.Value;
                plus = resultPlus.Value;
                return true;
            }
            else
                return false;
        }

        private static bool TLP_InRange(float rangeStart, float rangeEnd, float value) {
            return value >= rangeStart && value <= rangeEnd;
        }
        private static bool TLP_Project(Vector2 left, Vector2 right, Vector2 projectPoint, float maxDist, out Vector2 point) {
            float d = right.x - left.x;
            float ppd = projectPoint.x - left.x;

            float t1 = ppd / d;
            float lineY = (right.y - left.y) * t1 + left.y;

            point = new Vector2(projectPoint.x, (lineY + projectPoint.y) * 0.5f);
            return Math.Abs(projectPoint.y - lineY) <= maxDist;
        }

        //private static bool TLP_Projec_DO_ME(Vector2 left, Vector2 right, Vector2 pp, Vector2 ppTarget, float maxDist, out Vector2 point) {
        //    Debuger.AddDot(left, Color.yellow);
        //    Debuger.AddDot(right, Color.yellow);
        //    Debuger.AddDot(ppTarget, Color.magenta);

        //    point = Vector2.zero;

        //    if (ppTarget.x >= left.x | ppTarget.x <= right.x) {
        //        float d = right.x - left.x;
        //        float ppd = ppTarget.x - left.x;

        //        float lineD = ppd / d;
        //        float lineY = (right.y - left.y) * lineD + left.y;

        //        point = new Vector2(ppTarget.x, (lineY + ppTarget.y) * 0.5f);
        //        Debuger.AddDot(point, Color.cyan);

        //        if (Mathf.Abs(lineY - ppTarget.y) < maxDist)
        //            return true;
        //    }

        //    //Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2

        //    Vector2 v1 = right - left;
        //    Vector2 v2 = ppTarget - pp;

        //    float denominator = (v1.y * v2.x - v1.x * v2.y);

        //    if (denominator == 0) 
        //        return false;

        //    float t1 = ((left.x - pp.x) * v2.y + (pp.y - left.y) * v2.x) / denominator;
        //    Vector2 intersection = new Vector2(left.x + v1.x * t1, left.y + v1.y * t1);

        //    Vector2 targetDir = t1 < 0.5f ? Vector2.right : Vector2.left;

        //    Debuger.AddRay(intersection, targetDir, Color.red);
        //    Debuger.AddDot(intersection, Color.red);

        //    float angle1 = Vector2.Angle(v1, targetDir);
        //    float angle2 = Vector2.Angle(v2, targetDir);

        //    Vector2 upper, lower;
        //    float angle;

        //    if (angle1 > angle2) {
        //        angle = angle1;
        //        upper = v1;
        //        lower = v2;
        //    }
        //    else {
        //        angle = angle2;
        //        upper = v2;
        //        lower = v1;
        //    }

        //    float ulAngle = Vector2.Angle(v1, v2);

        //    upper = upper.normalized;
        //    lower = lower.normalized;

        //    return false;

        //    //Vector2 intersection;
        //    //if(LineIntersection3(left, right, pp, ppTarget, out intersection)) {
        //    //    Debuger.AddDot(intersection, Color.red);


        //    //}

        //    //point = Vector2.zero;

        //    //Vector2 r = new Vector2(1, 0);

        //    //Debuger.AddRay(left, r, Color.red);

        //    //Vector2 v1 = right - left;
        //    //Vector2 v2 = ppTarget - left;

        //    //float angle1 = Vector2.Angle(v1, r);
        //    //float angle2 = Vector2.Angle(v2, r);

        //    //Vector2 upper, lower;
        //    //float angle;

        //    //if (angle1 > angle2) {
        //    //    angle = angle1;
        //    //    upper = v1;
        //    //    lower = v2;
        //    //}
        //    //else {
        //    //    angle = angle2;
        //    //    upper = v2;
        //    //    lower = v1;
        //    //}

        //    //float ulAngle = Vector2.Angle(v1, v2);

        //    //upper = upper.normalized;
        //    //lower = lower.normalized;


        //    //var c = (1d / Math.Sin(angle * Mathf.Deg2Rad));

        //    //var topAngle = Vector2.Angle(Vector2.up, upper);
        //    //var a = maxDist * Math.Sin(topAngle * Mathf.Deg2Rad);
        //    //var c2 = a / Math.Sin(ulAngle * Mathf.Deg2Rad);

        //    //Debuger.AddLabel(left, topAngle);
        //    //Debuger.AddDot(left + (lower * (float)c2), Color.blue);
        //    //Debuger.AddRay(left + (lower * (float)c2), Vector2.up, Color.red, maxDist);


        //}

        public static bool TwoLinesProjectionX(Vector2 lineA1, Vector2 lineA2, Vector2 lineB1, Vector2 lineB2, float maxDistance, out Vector2 intersectionA, out Vector2 intersectionB) {
            SortedList<float, Vector2> results = new SortedList<float, Vector2>();

            ProjectionHelper(lineA1, lineA2, lineB1, maxDistance, ref results);
            ProjectionHelper(lineA1, lineA2, lineB2, maxDistance, ref results);
            ProjectionHelper(lineB1, lineB2, lineA1, maxDistance, ref results);
            ProjectionHelper(lineB1, lineB2, lineA2, maxDistance, ref results);

            if (results.Count == 2) {
                intersectionA = results.First().Value;
                intersectionB = results.Last().Value;
                return true;
            }

            if (results.Count > 2) {
                intersectionA = results.First().Value;
                intersectionB = results.Last().Value;
                Debug.Log("wat");
                return false;
            }

            intersectionA = Vector2.zero;
            intersectionB = Vector2.zero;
            return false;
        }

        private static void ProjectionHelper(Vector2 lineA, Vector2 lineB, Vector2 point, float maxDistance, ref SortedList<float, Vector2> list) {
            Vector2 intersection;
            if (point.x == lineA.x) {
                intersection = new Vector2(point.x, (point.y + lineA.y) * 0.5f);
                goto ADD_TO_LIST;
            }

            if (point.x == lineB.x) {
                intersection = new Vector2(point.x, (point.y + lineB.y) * 0.5f);
                goto ADD_TO_LIST;
            }

            Vector2 lineDirVec = lineB - lineA;
            Vector2 pointDirVec = point - lineA;

            if (lineDirVec.x > 0f ? InRangeExclusive(pointDirVec.x, 0f, lineDirVec.x) : InRangeExclusive(pointDirVec.x, lineDirVec.x, 0f)) {
                float val = pointDirVec.x / lineDirVec.x;
                if (Math.Abs((lineDirVec.y * val) - pointDirVec.y) < maxDistance) {
                    intersection = new Vector2(lineDirVec.x * val, ((lineDirVec.y * val) + pointDirVec.y) * 0.5f) + lineA;
                    goto ADD_TO_LIST;
                }
                else
                    return;
            }
            else
                return;

            ADD_TO_LIST:
            {
                if (list.ContainsKey(intersection.x) == false)
                    list.Add(intersection.x, intersection);
            }
        }
        #endregion

        public static bool ClosestToLineTopProjection(Vector2 point, Vector3 a, Vector3 b, float maxDifY, out Vector3 intersection) {
            Vector2 linePointA = new Vector2(a.x, a.z);
            Vector2 linePointB = new Vector2(b.x, b.z);

            linePointB = linePointA - linePointB;
            linePointB.Normalize();//this needs to be a unit vector
            Vector2 v = point - linePointA;
            var d = Vector2.Dot(v, linePointB);
            intersection = a + ((a - b).normalized * d);
            return true;
        }

        public static bool RayIntersectXZ(Vector2 rayOrigin, Vector2 rayDirection, Vector2 lineA, Vector2 lineB, out Vector2 lineIntersection) {
            float intersectX, intersectY, intersectZ;
            bool result = RayIntersectXZ(rayOrigin.x, rayOrigin.y, rayDirection.x, rayDirection.y, lineA.x, 0, lineA.y, lineB.x, 0, lineB.y, out intersectX, out intersectY, out intersectZ);
            lineIntersection = result ? new Vector2(intersectX, intersectZ) : new Vector2();
            return result;
        }        
        public static bool RayIntersectXZ(Vector3 rayOrigin, Vector3 rayDirection, Vector3 lineA, Vector3 lineB) {
            float intersectX, intersectY, intersectZ;
            return RayIntersectXZ(rayOrigin.x, rayOrigin.z, rayDirection.x, rayDirection.z, lineA.x, lineA.y, lineA.z, lineB.x, lineB.y, lineB.z, out intersectX, out intersectY, out intersectZ);
        }
        public static bool RayIntersectXZ(Vector3 rayOrigin, Vector3 rayDirection, Vector3 lineA, Vector3 lineB, out Vector3 lineIntersection) {
            float intersectX, intersectY, intersectZ;
            bool result = RayIntersectXZ(rayOrigin.x, rayOrigin.z, rayDirection.x, rayDirection.z, lineA.x, lineA.y, lineA.z, lineB.x, lineB.y, lineB.z, out intersectX, out intersectY, out intersectZ);
            lineIntersection = result ? new Vector3(intersectX, intersectY, intersectZ) : new Vector3();
            return result;
        }

        public static bool RayIntersectXZ(
            float rayOriginX, float rayOriginZ, float rayDirectionX, float rayDirectionZ,
            float lineA_x, float lineA_y, float lineA_z, float lineB_x, float lineB_y, float lineB_z,
            out float intersect_x, out float intersect_y, out float intersect_z) {
            float lineDir_x = lineB_x - lineA_x;
            float lineDir_y = lineB_y - lineA_y;
            float lineDir_z = lineB_z - lineA_z;
            float denominator = (lineDir_z * rayDirectionX - lineDir_x * rayDirectionZ);

            //paralel
            if (denominator == 0)
                goto NOPE;

            float t = ((lineA_x - rayOriginX) * rayDirectionZ + (rayOriginZ - lineA_z) * rayDirectionX) / denominator;

            if (t >= 0f && t <= 1f) {
                intersect_x = lineA_x + (lineDir_x * t);
                intersect_y = lineA_y + (lineDir_y * t);
                intersect_z = lineA_z + (lineDir_z * t);

                float dot =
                    (rayDirectionX * (intersect_x - rayOriginX)) +
                    (rayDirectionZ * (intersect_z - rayOriginZ));

                if (dot > 0)
                    return true;
                else
                    goto NOPE;
            }
            else
                goto NOPE;

            NOPE:
            {
                intersect_x = intersect_y = intersect_z = 0;
                return false;
            }
        }
        public static bool RayIntersect(
            float rayOriginX, float rayOriginY, 
            float rayDirectionX, float rayDirectionY,
            float lineA_x, float lineA_y, 
            float lineB_x, float lineB_y,
            out float intersect_x, 
            out float intersect_y) {
            float lineDir_x = lineB_x - lineA_x;
            float lineDir_y = lineB_y - lineA_y;
            float denominator = (lineDir_y * rayDirectionX - lineDir_x * rayDirectionY);

            //paralel
            if (denominator == 0)
                goto NOPE;

            float t = ((lineA_x - rayOriginX) * rayDirectionY + (rayOriginY - lineA_y) * rayDirectionX) / denominator;

            if (t >= 0f && t <= 1f) {
                intersect_x = lineA_x + (lineDir_x * t);
                intersect_y = lineA_y + (lineDir_y * t);

                float dot =
                    (rayDirectionX * (intersect_x - rayOriginX)) +
                    (rayDirectionY * (intersect_y - rayOriginY));

                if (dot > 0)
                    return true;
                else
                    goto NOPE;
            }
            else
                goto NOPE;

            NOPE:
            {
                intersect_x = intersect_y = 0;
                return false;
            }
        }


        //old version
        private static bool RayIntersectXZReadable(
            Vector3 rayOrigin, Vector3 rayDirection,
            Vector3 lineA, Vector3 lineB,
            out Vector3 lineIntersection) {

            Vector3 lineDirection = lineB - lineA;
            float denominator = (lineDirection.z * rayDirection.x - lineDirection.x * rayDirection.z);

            //paralel
            if (denominator == 0)
                goto NOPE;

            float t = ((lineA.x - rayOrigin.x) * rayDirection.z + (rayOrigin.z - lineA.z) * rayDirection.x) / denominator;

            if (t >= 0f && t <= 1f) {
                lineIntersection = lineA + (lineDirection * t);
                float dot =
                    (rayDirection.x * (lineIntersection.x - rayOrigin.x)) +
                    (rayDirection.z * (lineIntersection.z - rayOrigin.z));
                if (dot > 0)
                    return true;
                else
                    goto NOPE;
            }
            else
                goto NOPE;

            NOPE:
            {
                lineIntersection = Vector3.zero;
                return false;
            }
        }


        public static bool ClampedRayIntersectXZ(
        Vector3 rayOrigin, Vector3 rayDirection,
        Vector3 lineA, Vector3 lineB,
        out Vector3 lineIntersection) {

            Vector3 lineDirection = lineB - lineA;
            float denominator = (lineDirection.z * rayDirection.x - lineDirection.x * rayDirection.z);

            //lines are paralel
            if (denominator == 0) {
                lineIntersection = Vector3.zero;
                return false;
            }

            float t1 = ((lineA.x - rayOrigin.x) * rayDirection.z + (rayOrigin.z - lineA.z) * rayDirection.x) / denominator;
            bool result = t1 < 0f || t1 > 1f;
            t1 = Mathf.Clamp01(t1);


            lineIntersection = lineA + (lineDirection * t1);

            //float dot =
            //    (rayDirection.x * (lineIntersection.x - rayOrigin.x)) +
            //    (rayDirection.z * (lineIntersection.z - rayOrigin.z));

            return result;
        }

        public static bool LineIntersectXZ(Vector3 mainLineA, Vector3 mainLineB, Vector3 leadingLineA, Vector3 leadingLineB, out Vector3 lineIntersection) {
            Vector3 mainLineDirection = mainLineB - mainLineA;
            Vector3 leadingLineDirection = leadingLineB - leadingLineA;
            float denominator = (mainLineDirection.z * leadingLineDirection.x - mainLineDirection.x * leadingLineDirection.z);

            //paralel
            if (denominator == 0)
                goto NOPE;

            float t = ((mainLineA.x - leadingLineA.x) * leadingLineDirection.z + (leadingLineA.z - mainLineA.z) * leadingLineDirection.x) / denominator;

            if (t >= 0f && t <= 1f) {
                lineIntersection = mainLineA + (mainLineDirection * t);
                float dot = (leadingLineDirection.x * (lineIntersection.x - leadingLineA.x)) + (leadingLineDirection.z * (lineIntersection.z - leadingLineA.z));
                if (dot >= 0 &
                    Vector2.Distance(new Vector2(leadingLineA.x, leadingLineA.z), new Vector2(leadingLineB.x, leadingLineB.z)) >=
                    Vector2.Distance(new Vector2(leadingLineA.x, leadingLineA.z), new Vector2(lineIntersection.x, lineIntersection.z)))
                    return true;
                else
                    goto NOPE;
            }
            else
                goto NOPE;

            NOPE:
            {
                lineIntersection = Vector3.zero;
                return false;
            }
        }

        public static bool LineLineIntersectXZ(Vector3 mainLineA, Vector3 mainLineB, Vector3 leadingLineA, Vector3 leadingLineB, out Vector3 lineIntersection) {
            Vector3 mainLineDirection = mainLineB - mainLineA;
            Vector3 leadingLineDirection = leadingLineB - leadingLineA;
            float denominator = (mainLineDirection.z * leadingLineDirection.x - mainLineDirection.x * leadingLineDirection.z);

            //paralel
            if (denominator == 0) {
                lineIntersection = Vector3.zero;
                return false;
            }

            float t = ((mainLineA.x - leadingLineA.x) * leadingLineDirection.z + (leadingLineA.z - mainLineA.z) * leadingLineDirection.x) / denominator;
            lineIntersection = mainLineA + (mainLineDirection * t);
            return true;
        }

        public static bool LineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection) {
            float ix, iy;
            bool result = LineIntersection(a1.x, a1.y, a2.x, a2.y, b1.x, b1.y, b2.x, b2.y, out ix, out iy);
            intersection = new Vector2(ix, iy);
            return result;
        }

        public static bool LineIntersection(
            float a1x, float a1y, //line A 1
            float a2x, float a2y, //line A 2
            float b1x, float b1y, //line B 1
            float b2x, float b2y, //line B 2
            out float intersectionX, 
            out float intersectionY) {

            float aDirX = a2x - a1x;
            float aDirY = a2y - a1y;

            float bDirX = b2x - b1x;
            float bDirY = b2y - b1y;            

            float d = (aDirY * bDirX - aDirX * bDirY);

            if (d == 0) {
                intersectionX = intersectionY = 0f;
                return false;
            }

            float m = ((a1x - b1x) * bDirY + (b1y - a1y) * bDirX) / d;

            // Find the point of intersection.
            intersectionX = a1x + aDirX * m;
            intersectionY = a1y + aDirY * m;
            return true;
        }
    }
}
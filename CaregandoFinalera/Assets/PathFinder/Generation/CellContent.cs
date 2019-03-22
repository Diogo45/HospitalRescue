using K_PathFinder.EdgesNameSpace;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Graphs {
    //struct for storing two Vector3
    [Serializable]
    public struct CellContentData : IEquatable<CellContentData> {
        [SerializeField]
        private float 
            xLeft, yLeft, zLeft, //lefts
            xRight, yRight, zRight; //rights        

        public CellContentData(Vector3 left, Vector3 right) {
            xLeft = left.x;
            yLeft = left.y;
            zLeft = left.z;

            xRight = right.x;
            yRight = right.y;
            zRight = right.z;
        }
        public CellContentData(Vector3 point) {
            xLeft = point.x;
            yLeft = point.y;
            zLeft = point.z;

            xRight = point.x;
            yRight = point.y;
            zRight = point.z;
        }
        public CellContentData(EdgeAbstract edgeAbstract) : this(edgeAbstract.aPositionV3, edgeAbstract.bPositionV3) {}

        public static bool Project(CellContentData data_A, CellContentData data_B, float maxDist, Axis axis, out CellContentData intersection) {
            Vector2 a1 = new Vector2(axis == Axis.x ? data_A.a.x : data_A.a.z, data_A.a.y);
            Vector2 a2 = new Vector2(axis == Axis.x ? data_A.b.x : data_A.b.z, data_A.b.y);

            Vector2 b1 = new Vector2(axis == Axis.x ? data_B.a.x : data_B.a.z, data_B.a.y);
            Vector2 b2 = new Vector2(axis == Axis.x ? data_B.b.x : data_B.b.z, data_B.b.y);

            Vector2 i1, i2;
            if (SomeMath.TwoLinesProjectionByX(a1, a2, b1, b2, maxDist, out i1, out i2)) {
                if (axis == Axis.x) 
                    intersection = new CellContentData(new Vector3(i1.x, i1.y, data_A.a.z), new Vector3(i2.x, i2.y, data_A.a.z));                
                else 
                    intersection = new CellContentData(new Vector3(data_A.a.x, i1.y, i1.x), new Vector3(data_A.a.x, i2.y, i2.x));
                return true;
            }
            else {
                intersection = new CellContentData(Vector3.zero);
                return false;
            }
        }

        public static bool Project2(CellContentData data_A, CellContentData data_B, float maxDist, Axis axis, out CellContentData intersection, 
            ref List<CellContentData> aSideOutput, ref List<CellContentData> bSideOutput) {

            Vector3 aa = data_A.a;
            Vector3 ab = data_A.b;

            Vector3 ba = data_B.a;
            Vector3 bb = data_B.b;

            if (aa.x > ab.x) {
                Vector3 temp = ab;
                ab = aa;
                aa = temp;
            }

            if (ba.x > bb.x) {
                Vector3 temp = bb;
                bb = ba;
                ba = temp;
            }

            Vector2 a1 = new Vector2(axis == Axis.x ? aa.x : aa.z, aa.y);
            Vector2 a2 = new Vector2(axis == Axis.x ? ab.x : ab.z, ab.y);

            Vector2 b1 = new Vector2(axis == Axis.x ? ba.x : ba.z, ba.y);
            Vector2 b2 = new Vector2(axis == Axis.x ? bb.x : bb.z, bb.y);



            Vector2 minus, plus;
            if (SomeMath.TwoLinesProjectionByX(a1, a2, b1, b2, maxDist, out minus, out plus)) {

                if (axis == Axis.x)
                    intersection = new CellContentData(new Vector3(minus.x, minus.y, data_A.a.z), new Vector3(plus.x, plus.y, data_A.a.z));
                else
                    intersection = new CellContentData(new Vector3(data_A.a.x, minus.y, minus.x), new Vector3(data_A.a.x, plus.y, plus.x));


                bSideOutput.Add(intersection);
                aSideOutput.Add(intersection);

                if(intersection.xLeft > aa.x) 
                    aSideOutput.Add(new CellContentData(aa, intersection.leftV3));                
                if(intersection.xRight < ab.x) 
                    aSideOutput.Add(new CellContentData(ab, intersection.rightV3));                


                if (intersection.xLeft > ba.x) 
                    aSideOutput.Add(new CellContentData(ba, intersection.leftV3));                
                if (intersection.xRight < bb.x) 
                    aSideOutput.Add(new CellContentData(bb, intersection.rightV3));
                                
                return true;
            }
            else {
                intersection = new CellContentData(Vector3.zero);
                return false;
            }
        }

        public bool Contains(Vector3 v3) {
            return 
                (xLeft == v3.x && yLeft == v3.y && zLeft == v3.z) | 
                (xRight == v3.x && yRight == v3.y && zRight == v3.z);
        }
        public bool pointed {
            get { return 
                    xLeft == xRight && 
                    yLeft == yRight && 
                    zLeft == zRight; }
        }
                
        /// <summary>
        /// x,y,z
        /// </summary>
        public Vector3 leftV3 {
            get { return new Vector3(xLeft, yLeft, zLeft); }
        }
        /// <summary>
        /// x,y,z
        /// </summary>
        public Vector3 rightV3 {
            get { return new Vector3(xRight, yRight, zRight); }
        }

        /// <summary>
        /// x,z
        /// </summary>
        public Vector2 leftV2 {
            get { return new Vector2(xLeft, zLeft); }
        }
        /// <summary>
        /// x,z
        /// </summary>
        public Vector2 rightV2 {
            get { return new Vector2(xRight, zRight); }
        }
        
        /// <summary>
        /// left
        /// </summary>
        public Vector3 a {
            get { return new Vector3(xLeft, yLeft, zLeft); }
        }
        /// <summary>
        /// right
        /// </summary>
        public Vector3 b {
            get { return new Vector3(xRight, yRight, zRight); }
        }

        /// <summary>
        /// x,y,z
        /// </summary>
        public Vector3 centerV3 {
            get { return new Vector3((xLeft + xRight) * 0.5f, (yLeft + yRight) * 0.5f, (zLeft + zRight) * 0.5f); }
        }
        /// <summary>
        /// x,z
        /// </summary>
        public Vector3 centerV2 {
            get { return new Vector3((xLeft + xRight) * 0.5f, (zLeft + zRight) * 0.5f); }
        }

        public static bool operator ==(CellContentData a, CellContentData b) {
            return (a.leftV3 == b.leftV3 && a.rightV3 == b.rightV3) | (a.leftV3 == b.rightV3 && a.rightV3 == b.leftV3);
        }
        public static bool operator !=(CellContentData a, CellContentData b) {
            return !(a == b);
        }

        public override int GetHashCode() {
            return (int)(xLeft + yLeft + zLeft + xRight + yRight + zRight);
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is CellContentData))
                return false;

            return Equals((CellContentData)obj);
        }

        public bool Equals(CellContentData other) {
            return this == other;
        }

        public override string ToString() {
            return string.Format("(V1: {0} V2: {1})", leftV3, rightV3);
        }
    }

    [Serializable]
    public struct CellContentDataShort : IEquatable<CellContentDataShort> {
        [SerializeField]
        private float
            xLeft, yLeft, zLeft, //lefts
            xRight, yRight, zRight; //rights        

        public CellContentDataShort(Vector3 left, Vector3 right) {
            xLeft = left.x;
            yLeft = left.y;
            zLeft = left.z;

            xRight = right.x;
            yRight = right.y;
            zRight = right.z;
        }
        public CellContentDataShort(Vector3 point) {
            xLeft = point.x;
            yLeft = point.y;
            zLeft = point.z;

            xRight = point.x;
            yRight = point.y;
            zRight = point.z;
        }
        public CellContentDataShort(EdgeAbstract edgeAbstract) : this(edgeAbstract.aPositionV3, edgeAbstract.bPositionV3) { }
        public CellContentDataShort(CellContentData ccd) : this(ccd.leftV3, ccd.rightV3) { }


        /// <summary>
        /// x,y,z
        /// </summary>
        public Vector3 leftV3 {
            get { return new Vector3(xLeft, yLeft, zLeft); }
        }
        /// <summary>
        /// x,y,z
        /// </summary>
        public Vector3 rightV3 {
            get { return new Vector3(xRight, yRight, zRight); }
        }

        /// <summary>
        /// x,z
        /// </summary>
        public Vector2 leftV2 {
            get { return new Vector2(xLeft, zLeft); }
        }
        /// <summary>
        /// x,z
        /// </summary>
        public Vector2 rightV2 {
            get { return new Vector2(xRight, zRight); }
        }

        /// <summary>
        /// left
        /// </summary>
        public Vector3 a {
            get { return new Vector3(xLeft, yLeft, zLeft); }
        }
        /// <summary>
        /// right
        /// </summary>
        public Vector3 b {
            get { return new Vector3(xRight, yRight, zRight); }
        }

        /// <summary>
        /// x,y,z
        /// </summary>
        public Vector3 centerV3 {
            get { return new Vector3((xLeft + xRight) * 0.5f, (yLeft + yRight) * 0.5f, (zLeft + zRight) * 0.5f); }
        }
        /// <summary>
        /// x,z
        /// </summary>
        public Vector3 centerV2 {
            get { return new Vector3((xLeft + xRight) * 0.5f, (zLeft + zRight) * 0.5f); }
        }

        public static bool operator ==(CellContentDataShort a, CellContentDataShort b) {
            return a.leftV3 == b.leftV3 && a.rightV3 == b.rightV3;
        }
        public static bool operator !=(CellContentDataShort a, CellContentDataShort b) {
            return !(a == b);
        }

        public override int GetHashCode() {
            return (int)(xLeft + yLeft + zLeft + xRight + yRight + zRight);
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is CellContentDataShort))
                return false;

            return Equals((CellContentDataShort)obj);
        }

        public bool Equals(CellContentDataShort other) {
            return this == other;
        }

        public override string ToString() {
            return string.Format("(V1: {0} V2: {1})", leftV3, rightV3);
        }
    }

    public abstract class CellContent {
        protected readonly CellContentData _cellData;
        protected readonly bool _interconnection;
        protected readonly Cell _from, _to;
        protected readonly float _costFrom, _costTo;

        public CellContent(CellContentData cellData, Cell from, Cell to, bool interconnection, float costFrom, float costTo) {
            _cellData = cellData;
            _from = from;
            _to = to;
            _interconnection = interconnection;
            _costFrom = costFrom;
            _costTo = costTo;
        }

        public abstract float Cost(AgentProperties properties, bool ignoreCrouchCost);
        public abstract float Cost(Vector3 fromPos, AgentProperties properties, bool ignoreCrouchCost);

        public CellContentData cellData {
            get { return _cellData; }
        }
        public bool interconnection {
            get { return _interconnection; }
        }
        public Cell from {
            get { return _from; }
        }
        public Cell connection {
            get { return _to; }
        }

        public float costFrom {
            get { return _costFrom; }
        }
        public float costTo {
            get { return _costTo; }
        }

        //left
        public Vector3 left {
            get { return _cellData.leftV3; }
        }
        public Vector2 leftV2 {
            get { return _cellData.leftV2; }
        }
        public Vector3 leftV3 {
            get { return _cellData.leftV3; }
        }

        //right
        public Vector3 right {
            get { return _cellData.rightV3; }
        }
        public Vector2 rightV2 {
            get { return _cellData.rightV2; }
        }
        public Vector3 rightV3 {
            get { return _cellData.rightV3; }
        }
    }

    //generic walking
    public class CellContentGenericConnection : CellContent {
        private readonly Vector3 _intersection;

        public CellContentGenericConnection(
            CellContentData cellData, Cell from, Cell to, bool interconnection, float costFrom, float costTo,
            Vector3 intersection) : base(cellData, from, to, interconnection, costFrom, costTo) {
            _intersection = intersection;
        }

        public Vector3 intersection {
            get { return _intersection; }
        }

        public override float Cost(AgentProperties properties, bool ignoreCrouchCost) {
            float result = 0;
            switch (from.passability) {
                case Passability.Crouchable:
                    if(ignoreCrouchCost)
                        result += properties.walkMod * _costFrom;
                    else
                        result += properties.crouchMod * _costFrom;
                    break;
                case Passability.Walkable:
                    result += properties.walkMod * _costFrom;
                    break;
                default:
                    Debug.LogWarning("wrong passability in cost mod");
                    break;
            }

            switch (connection.passability) {
                case Passability.Crouchable:
                    if(ignoreCrouchCost)
                        result += properties.walkMod * _costTo;
                    else
                        result += properties.crouchMod * _costTo;
                    break;
                case Passability.Walkable:
                    result += properties.walkMod * _costTo;
                    break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                    break;
            }
            return result;
        }
        public override float Cost(Vector3 fromPos, AgentProperties properties, bool ignoreCrouchCost) {
            float result = 0;

            switch (connection.passability) {
                case Passability.Crouchable:
                    if(ignoreCrouchCost)
                        result += properties.walkMod * _costTo;
                    else
                        result += properties.crouchMod * _costTo;
                    break;
                case Passability.Walkable:
                    result += properties.walkMod * _costTo;
                    break;
                default:
                    UnityEngine.Debug.LogWarning("wrong passability in cost mod");
                    break;
            }
            return result;
        }
    }

    //jumps
    //Axis point is CellContentData in base class
    public class CellContentPointedConnection : CellContent {
        private readonly Vector3 _enterPoint, _lowerStandingPoint, _exitPoint;
        private readonly ConnectionJumpState _state;

        public CellContentPointedConnection(Vector3 enterPoint, Vector3 lowerStandingPoint, Vector3 exitPoint, Vector3 axis, ConnectionJumpState state, Cell from, Cell to, bool interconnection)
            : base(new CellContentData(axis), from, to, interconnection,
                  Vector3.Distance(from.centerV3, enterPoint),
                  Vector3.Distance(to.centerV3, exitPoint)) {
            _enterPoint = enterPoint;
            _lowerStandingPoint = lowerStandingPoint;
            _exitPoint = exitPoint;
            _state = state;
        }

        public Vector3 enterPoint {
            get { return _enterPoint; }
        }
        public Vector3 lowerStandingPoint {
            get { return _lowerStandingPoint; }
        }
        public Vector3 exitPoint {
            get { return _exitPoint; }
        }
        public Vector3 axis {
            get { return cellData.leftV3; }
        }
        public ConnectionJumpState jumpState {
            get { return _state; }
        }

        public override float Cost(AgentProperties properties, bool ignoreCrouchCost) {
            float result = 0;
            switch (from.passability) {
                case Passability.Crouchable:
                    if(ignoreCrouchCost)
                        result += properties.walkMod * _costFrom;
                    else
                        result += properties.crouchMod * _costFrom;
                    break;
                case Passability.Walkable:
                    result += properties.walkMod * _costFrom;
                    break;
                default:
                    Debug.LogWarning("wrong passability in cost mod");
                    break;
            }

            switch (connection.passability) {
                case Passability.Crouchable:
                    if(ignoreCrouchCost)
                        result += properties.walkMod * _costTo;
                    else
                        result += properties.crouchMod * _costTo;
                    break;
                case Passability.Walkable:
                    result += properties.walkMod * _costTo;
                    break;
                default:
                    Debug.LogWarning("wrong passability in cost mod");
                    break;
            }

            result += properties.jumpUpMod;
            return result;
        }
        public override float Cost(Vector3 fromPos, AgentProperties properties, bool ignoreCrouchCost) {
            float result = 0;
            switch (from.passability) {
                case Passability.Crouchable:
                    if(ignoreCrouchCost)
                        result += properties.walkMod * Vector3.Distance(fromPos, enterPoint);
                    else
                        result += properties.crouchMod * Vector3.Distance(fromPos, enterPoint);
                    break;
                case Passability.Walkable:
                    result += properties.walkMod * Vector3.Distance(fromPos, enterPoint);
                    break;
                default:
                    Debug.LogWarning("wrong passability in cost mod");
                    break;
            }

            switch (connection.passability) {
                case Passability.Crouchable:
                    if (ignoreCrouchCost)
                        result += properties.walkMod * _costTo;
                    else
                        result += properties.crouchMod * _costTo;
                    break;
                case Passability.Walkable:
                    result += properties.walkMod * _costTo;
                    break;
                default:
                    Debug.LogWarning("wrong passability in cost mod");
                    break;
            }

            result += properties.jumpUpMod;
            return result;
        }
    }
}
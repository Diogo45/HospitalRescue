using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using K_PathFinder.VectorInt;
using K_PathFinder.Graphs;
using K_PathFinder;
using System;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif
namespace K_PathFinder {
    public struct GeneralXZData : IEquatable<GeneralXZData> {
        public readonly XZPosInt chunkPos;
        public readonly AgentProperties properties;

        public GeneralXZData(XZPosInt chunkPos, AgentProperties properties) {
            this.chunkPos = chunkPos;
            this.properties = properties;
        }
        public GeneralXZData(int x, int z, AgentProperties properties) {
            this.chunkPos = new XZPosInt(x,z);
            this.properties = properties;
        }

        public override int GetHashCode() {
            return chunkPos.GetHashCode() ^ properties.GetHashCode();
        }

        public bool Equals(GeneralXZData other) {
            return other == this;
        }

        public override bool Equals(object obj) {
            if (!(obj is GeneralXZData))
                return false;

            return (GeneralXZData)obj == this;
        }

        public static bool operator ==(GeneralXZData a, GeneralXZData b) {
            return a.properties == b.properties && a.chunkPos == b.chunkPos;
        }

        public static bool operator !=(GeneralXZData a, GeneralXZData b) {
            return !(a == b);
        }
    }   

    [Serializable]
    public struct YRangeInt{
        public int min, max;
        public YRangeInt(int min, int max) {
            this.min = min;
            this.max = max;
        }

        public override int GetHashCode() {
            return min ^ (max * 100);
        }

        public bool Equals(YRangeInt other) {
            return other == this;
        }

        public override bool Equals(object obj) {
            if (!(obj is YRangeInt))
                return false;

            return (YRangeInt)obj == this;
        }

        public static bool operator ==(YRangeInt a, YRangeInt b) {
            return a.min == b.min && a.max == b.max;
        }

        public static bool operator !=(YRangeInt a, YRangeInt b) {
            return !(a == b);
        }
    }
    [Serializable]
    public struct XZPosInt : IEquatable<XZPosInt> {
        public int x, z;
        public XZPosInt(int x, int z) {
            this.x = x;
            this.z = z;
        }

        public override int GetHashCode() {
            return x ^ (z * 100);
        }

        public bool Equals(XZPosInt other) {
            return other == this;
        }

        public override bool Equals(object obj) {
            if (!(obj is XZPosInt))
                return false;

            return (XZPosInt)obj == this;
        }

        public static bool operator ==(XZPosInt a, XZPosInt b) {
            return a.x == b.x && a.z == b.z;
        }

        public static bool operator !=(XZPosInt a, XZPosInt b) {
            return !(a == b);
        }

        public override string ToString() {
            return string.Format("x: {0}, z: {1}", x, z);
        }
    }

    public struct ChunkData {  
        public readonly int x, z, min, max;

        public ChunkData(int x, int z, int min, int max) {
            this.x = x;
            this.z = z;
            this.min = min;
            this.max = max;
        }
        public ChunkData(VectorInt.Vector2Int position, int min, int max) : this(position.x, position.y, min, max) {}

        public ChunkData(XZPosInt pos, YRangeInt range) : this(pos.x, pos.z, range.min, range.max) { }


        public float realX {
            get { return x * PathFinder.gridSize; ; }
        }
        public float realZ {
            get { return z * PathFinder.gridSize; }
        }

        public XZPosInt xzPos {
            get { return new XZPosInt(x, z); }
        }

        public VectorInt.Vector2Int position {
            get { return new VectorInt.Vector2Int(x, z); }
        }

        public Vector3 realPositionV3 {
            get { return new Vector3(realX, 0, realZ); }
        }

        public Vector2 realPositionV2 {
            get { return new Vector3(realX, realZ); }
        }

        public float realMin {
            get { return min * PathFinder.gridSize; ; }
        }
        public float realMax {
            get { return max * PathFinder.gridSize; }
        }

        public Vector3 boundSize {
            get {
                float gridSize = PathFinder.gridSize;
                float minY = min * gridSize;
                float maxY = max * gridSize + gridSize;
                return new Vector3(gridSize, Math.Abs(minY - maxY), gridSize);
            }
        }
        public Bounds bounds {
            get { return new Bounds(centerV3, boundSize); }
        }

        public Vector3 centerV3 {
            get { return new Vector3(realX, realMin, realZ) + (boundSize * 0.5f); }
        }
        /// <summary>
        /// x = x, z = y
        /// </summary>
        public Vector2 centerV2 {
            get { return new Vector2(realX + (PathFinder.gridSize * 0.5f), realZ + (PathFinder.gridSize * 0.5f)); }
        }

        public string positionString {
            get { return "x:" + x + ", z:" + z; }
        }
        public string heightString {
            get { return "min:" + min + ", max:" + max; }
        }
        public string positionHeightString {
            get { return positionString + ", " + heightString; }
        }

    }


    //public class Chunk {
    //    /// <summary>
    //    /// x == x
    //    /// y == z
    //    /// </summary>
    //    public Vector2Int position { get; private set; }
    //    public int min, max;
        
    //    private Dictionary<AgentProperties, Graph> _graphs = new Dictionary<AgentProperties, Graph>();
    //    private Chunk[] _neighbours = new Chunk[4];

    //    public Chunk(Vector2Int position, int min, int max) {
    //        this.position = position;
    //        this.min = min;
    //        this.max = max;
    //    }

    //    public int x {
    //        get { return position.x; }
    //    }
    //    public int z {
    //        get { return position.y; }
    //    }

    //    public float realX {
    //        get { return x * PathFinder.gridSize; ; }
    //    }
    //    public float realZ {
    //        get { return z * PathFinder.gridSize; }
    //    }

    //    public Vector3 realPositionV3 {
    //        get {return new Vector3(realX, 0, realZ);}
    //    }

    //    public Vector2 realPositionV2 {
    //        get { return new Vector3(realX, realZ); }
    //    }

    //    public float realMin {
    //        get { return min * PathFinder.gridSize; ; }
    //    }
    //    public float realMax {
    //        get { return max * PathFinder.gridSize; }
    //    }

    //    public Vector3 boundSize {
    //        get {
    //            float gridSize = PathFinder.gridSize;
    //            float minY = min * gridSize;
    //            float maxY = max * gridSize + gridSize;
    //            return new Vector3(gridSize, Math.Abs(minY - maxY), gridSize);
    //        }
    //    }
    //    public Bounds bounds {
    //        get {return new Bounds(centerV3, boundSize); }
    //    }

    //    public Vector3 centerV3 {
    //        get { return new Vector3(realX, realMin, realZ) + (boundSize * 0.5f); }
    //    }
    //    /// <summary>
    //    /// x = x, z = y
    //    /// </summary>
    //    public Vector2 centerV2 {
    //        get { return new Vector2(realX + (PathFinder.gridSize * 0.5f), realZ + (PathFinder.gridSize * 0.5f)); }
    //    }

    //    public string positionString {
    //        get { return "x:" + x + ", z:" + z; }
    //    }
    //    public string heightString {
    //        get { return "min:" + min + ", max:" + max; }
    //    }
    //    public string positionHeightString {
    //        get { return positionString + ", " + heightString; }
    //    }

    //    public IEnumerable<AgentProperties> properties {
    //        get { return _graphs.Keys; }
    //    }

    //    public bool TryGetGraph(AgentProperties properties, out Graph graph) {
    //        return _graphs.TryGetValue(properties, out graph);
    //    }
    //    public void SetGraph(AgentProperties properties, Graph graph) {
    //        if (_graphs.ContainsKey(properties)) {
    //            _graphs[properties] = graph;
    //        }
    //        else {
    //            _graphs.Add(properties, graph);
    //        }
    //    }

    //    public Chunk GetNeighbour(Directions direction) {
    //        return _neighbours[(int)direction];
    //    }
    //    public bool TryGetNeighbour(Directions direction, AgentProperties properties, out Chunk chunk, out Graph graph) {
    //        chunk = _neighbours[(int)direction];
    //        if(chunk == null) {
    //            graph = null;
    //            return false;
    //        }
    //        else 
    //            return chunk.TryGetGraph(properties, out graph);            
    //    }
    //    public bool TryGetNeighbour(Directions direction, AgentProperties properties, out Graph graph) {
    //        Chunk chunk;
    //        return TryGetNeighbour(direction, properties, out chunk, out graph);
    //    }

    //    public bool TryGetNeighbour(Directions direction, out Chunk result) {
    //        result = _neighbours[(int)direction];
    //        return result != null;
    //    }

    //    public void SetNeighbour(Directions direction, Chunk chunk) {
    //        _neighbours[(int)direction] = chunk;
    //        chunk._neighbours[(int)Enums.Opposite(direction)] = this;
    //    }

    //    public void RemoveGraph(AgentProperties properties) {
    //        _graphs.Remove(properties);
    //    }

    //    public string DescribeNeightbours() {
    //        string nulls = "";
    //        string notNulls = "";
    //        for (int i = 0; i < 4; i++) {
    //            if (_neighbours[i] == null) {
    //                if (nulls != "")
    //                    nulls += ", ";
    //                nulls += ((Directions)i).ToString();
    //            }
    //            else {
    //                if (notNulls != "")
    //                    notNulls += ", ";
    //                notNulls += ((Directions)i).ToString();
    //            }
    //        }
    //        string result = "Connections:\n";
    //        result += ("Exist: " + notNulls + "\n");
    //        result += ("Not exist: " + nulls + "\n");
    //        return result;
    //    }
    //}
}

using UnityEngine;
using System.Collections.Generic;
using K_PathFinder.CoverNamespace;

namespace K_PathFinder.Graphs {
    //convex mesh with some additional data
    public class Cell {
        public int layer { get; private set; }//cell layer. dont need outside generation but will be needed if i will do dynamic obstacles
        public Area area { get; private set; }//cell area reference

        public Passability passability { get; private set; }
        public Graph graph { get; private set; }//graph it's belong. dont need outside generation but still can have some uses

        //cell center are center of it's mesh area
        public Vector3 centerV3 { get; private set; }
        public Vector2 centerV2 { get; private set; }

        public HashSet<NodeCoverPoint> covers { get; private set; }
        public bool canBeUsed; //if not it will be ignored in path generation. cause creating navmesh done in threads and removing are not instant

        Dictionary<CellContentData, CellContent> _contentDictionary = new Dictionary<CellContentData, CellContent>();//cell edges
        List<CellContent> _connections = new List<CellContent>();//cell connections
        List<CellContentData> _originalEdges; //just cell edges without jump spots to recreate cell if needed

        public Cell(Area area, Passability passability, int layer, Graph graph, IEnumerable<CellContentData> originalEdges) {
            this.area = area;
            this.passability = passability;
            this.layer = layer;
            this.graph = graph;
            _originalEdges = new List<CellContentData>(originalEdges);
            foreach (var oe in originalEdges) {
                _contentDictionary.Add(oe, null);
            }
        }

        public void SetCenter(Vector3 center) {
            centerV3 = center;
            centerV2 = new Vector2(center.x, center.z);
        }
        public void AddCover(NodeCoverPoint cover) {
            if (covers == null)
                covers = new HashSet<NodeCoverPoint>();

            covers.Add(cover);
        }
        public void SetAsCanBeUsed() {
            canBeUsed = true;
        }

        #region data
        public void RemoveAllConnections(Cell target) {         
            for (int i = _connections.Count - 1; i >= 0; i--) {
                if (_connections[i].connection == target) {
                    CellContentData data = _connections[i].cellData;                
                    _connections.RemoveAt(i);
                   
                    //if edge was presented before we add this connection then it will remain in dictionary
                    //else we remove it
                    if (_originalEdges.Contains(data)) {
                        _contentDictionary[data] = null;
                    }
                    else {
                        _contentDictionary.Remove(data);
                    }
                }
            }
        }
        public void TryAddData(CellContentData d) {
            if (!_contentDictionary.ContainsKey(d))
                _contentDictionary.Add(d, null);
        }
        public void TryAddData(IEnumerable<CellContentData> ds) {
            foreach (var d in ds) {
                if (!_contentDictionary.ContainsKey(d))
                    _contentDictionary.Add(d, null);
            }
        }
        public void SetContent(CellContent cc) {
            _contentDictionary.Remove(cc.cellData); //make sure sides are on correct side
            _contentDictionary.Add(cc.cellData, cc);
            _connections.Add(cc);
        }
        public void Remove(CellContentData d) {
            CellContent c = _contentDictionary[d];
            if (c != null)
                _connections.Remove(c);
            if (!_contentDictionary.Remove(d)) {
                Debug.LogError("false");
            }
        }
        public bool Contains(CellContentData data) {
            return _contentDictionary.ContainsKey(data);
        }
        #endregion

        #region acessors
        public IEnumerable<CellContentData> originalEdges {
            get { return _originalEdges; }
        }
        public IEnumerable<KeyValuePair<CellContentData, CellContent>> dataContentPairs {
            get { return _contentDictionary; }
        }
        public IEnumerable<CellContentData> data {
            get { return _contentDictionary.Keys; }
        }
        public IEnumerable<CellContent> connections {
            get { return _connections; }
        }
        #endregion

        #region closest
        //Vector2
        public bool GetPointInsideCell(Vector2 targetPos, out Vector3 result) {
            foreach (var edgeData in _contentDictionary.Keys) {
                if (SomeMath.PointInTriangle(edgeData.leftV2, edgeData.rightV2, centerV2, targetPos)) {
                    result = new Vector3(targetPos.x, SomeMath.CalculateHeight(edgeData.leftV3, edgeData.rightV3, centerV3, targetPos.x, targetPos.y), targetPos.y);
                    return true;
                }
            }
            result = Vector3.zero;
            return false;
        }
        //vector3
        public bool GetPointInsideCell(Vector3 targetPos, out Vector3 result) {
            return GetPointInsideCell(new Vector2(targetPos.x, targetPos.z), out result);
        }

        //Vector2
        public void GetClosestPointToCell(Vector2 targetPos, out Vector3 closestPoint, out bool isOutsideCell) {
            float closestSqrDistance = float.MaxValue;
            closestPoint = Vector3.zero;

            foreach (var edgeData in _contentDictionary.Keys) {
                if (SomeMath.PointInTriangle(edgeData.leftV2, edgeData.rightV2, centerV2, targetPos)) {
                    closestPoint = new Vector3(targetPos.x, SomeMath.CalculateHeight(edgeData.leftV3, edgeData.rightV3, centerV3, targetPos.x, targetPos.y), targetPos.y);
                    isOutsideCell = false;
                    return;
                }
                else {
                    Vector3 curInte;
                    SomeMath.ClosestToSegmentTopProjection(edgeData.leftV3, edgeData.rightV3, targetPos, true, out curInte);
                    float curSqrDist = SomeMath.SqrDistance(targetPos, new Vector2(curInte.x, curInte.z));

                    if (curSqrDist < closestSqrDistance) {
                        closestSqrDistance = curSqrDist;
                        closestPoint = curInte;
                    }                    
                }
            }
            isOutsideCell = true;
            return;
        }
        //vector3
        public void GetClosestPointToCell(Vector3 targetPos, out Vector3 closestPoint, out bool isOutsideCell) {
            GetClosestPointToCell(new Vector2(targetPos.x, targetPos.z), out closestPoint, out isOutsideCell);
        }     
        #endregion
    }
}

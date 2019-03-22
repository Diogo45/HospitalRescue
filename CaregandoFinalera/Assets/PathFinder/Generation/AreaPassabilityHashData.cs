using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace K_PathFinder {
    /// <summary>
    /// this thing stored dictionary of area and passability as int and transfer it back and forth
    /// it exist this way cause PathFinder have one static AreaPassabilityHashData, but when needed it can clone current 
    /// main AreaPassabilityHashData to diferent threads. cause it was locked big chunk of time it cost some perfomance
    /// so this is way around.
    /// also a bit more readable
    /// </summary>
    public class AreaPassabilityHashData {
        private HashSet<Area> _areaPool;
        private Dictionary<AreaPassabilityPair, int> _areaToHash;
        private Dictionary<int, AreaPassabilityPair> _hashToArea;

        public AreaPassabilityHashData() {
            _areaPool = new HashSet<Area>();
            _areaToHash = new Dictionary<AreaPassabilityPair, int>();
            _hashToArea = new Dictionary<int, AreaPassabilityPair>();
        }

        private AreaPassabilityHashData(AreaPassabilityHashData origin) {
            _areaPool = new HashSet<Area>(origin._areaPool);
            _areaToHash = new Dictionary<AreaPassabilityPair, int>(origin._areaToHash);
            _hashToArea = new Dictionary<int, AreaPassabilityPair>(origin._hashToArea);
        }

        public void AddAreaHash(Area area) {
            if (area == null)
                Debug.LogError("you try to create area hash using null");

            if (_areaPool.Add(area)) {
                //adding this pairs
                AreaPassabilityPair crouchable = new AreaPassabilityPair(area, Passability.Crouchable);
                AreaPassabilityPair walkable = new AreaPassabilityPair(area, Passability.Walkable);        
                //reson is + 1 cause we can use ID:0 to tell "just do nothing" instead of ID:-1 later on (in layer hashmap for example)
                int crouchableKey = _areaToHash.Count + 1;
                int walkableKey = _areaToHash.Count + 2;
                
                _areaToHash.Add(crouchable, crouchableKey);
                _areaToHash.Add(walkable, walkableKey);

                _hashToArea.Add(crouchableKey, crouchable);
                _hashToArea.Add(walkableKey, walkable);
            }
        }

        public int GetAreaHash(Area area, Passability passability) {
            return _areaToHash[new AreaPassabilityPair(area, passability)];
        }

        public void GetAreaByHash(int value, out Area area, out Passability passability) {
            AreaPassabilityPair val = _hashToArea[value];
            area = val.area;
            passability = val.passability;
        }

        public string DescribeHashes() {
            StringBuilder sb = new StringBuilder();
            foreach (var item in _areaToHash) {
                sb.AppendFormat("area: {0}, passbility: {1}, hash: {2} \n", item.Key.area.name, item.Key.passability, item.Value);
            }
            return sb.ToString();
        }

        public AreaPassabilityHashData Clone() {
            return new AreaPassabilityHashData(this);
        }
    }


    public struct AreaPassabilityPair : IEqualityComparer<AreaPassabilityPair> {
        public Area area;
        public Passability passability;

        public AreaPassabilityPair(Area area, Passability passability) {
            this.area = area;
            this.passability = passability;
        }

        public static bool operator ==(AreaPassabilityPair a, AreaPassabilityPair b) {
            return ReferenceEquals(a.area, b.area) && a.passability == b.passability;
        }

        public static bool operator !=(AreaPassabilityPair a, AreaPassabilityPair b) {
            return !(a == b);
        }

        public override bool Equals(object obj) {
            if (obj is AreaPassabilityPair == false)
                return false;

            return (AreaPassabilityPair)obj == this;
        }

        public bool Equals(AreaPassabilityPair a, AreaPassabilityPair b) {
            return ReferenceEquals(a.area, b.area) && a.passability == b.passability;
        }

        public override int GetHashCode() {
            return area.GetHashCode() ^ ((int)passability * 500000);
        }

        public int GetHashCode(AreaPassabilityPair obj) {
            return obj.GetHashCode();
        }
    }
}

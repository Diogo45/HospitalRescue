using System;

namespace K_PathFinder {
    //simple struct to hold ints

    //x, z
    public struct VolumePosSmall : IEquatable<VolumePosSmall> {
        public readonly int x, z;

        public VolumePosSmall(int x, int z) {
            this.x = x;
            this.z = z;
        }

        public VolumePosSmall(VolumePos pos) {
            x = pos.x;
            z = pos.z;
        }

        //operators
        public static bool operator ==(VolumePosSmall a, VolumePosSmall b) {
            return a.x == b.x && a.z == b.z;
        }
        public static bool operator !=(VolumePosSmall a, VolumePosSmall b) {
            return !(a == b);
        }

        //equality
        public override bool Equals(object obj) {
            if (obj == null || !(obj is VolumePosSmall))
                return false;

            return Equals((VolumePosSmall)obj);
        }
        public bool Equals(VolumePosSmall other) {
            return x == other.x && z == other.z;
        }

        public override int GetHashCode() {
            return x ^ z;
        }

        public override string ToString() {
            return string.Format("(x: {0}, z: {1})", x, z);
        }
    }

    //volume, x, z
    public struct VolumePos : IEquatable<VolumePos> {
        public readonly int volume, x, z;

        public VolumePos(int Volume, int X, int Z) {
            volume = Volume;
            x = X;
            z = Z;
        }

        //operators
        public static bool operator ==(VolumePos a, VolumePos b) {
            return a.volume == b.volume && a.x == b.x && a.z == b.z;
        }
        public static bool operator !=(VolumePos a, VolumePos b) {
            return !(a == b);
        }

        //equality
        public override bool Equals(object obj) {
            if (obj == null || !(obj is VolumePos))
                return false;

            return Equals((VolumePos)obj);
        }
        public bool Equals(VolumePos other) {
            return volume == other.volume && x == other.x && z == other.z;
        }
        
        public override int GetHashCode() {
            return volume ^ (x * 123) ^ z * (321);
        }

        public override string ToString() {
            return string.Format("volume: {0}, x: {1}, z: {2}", volume, x, z);
        }
    }
}
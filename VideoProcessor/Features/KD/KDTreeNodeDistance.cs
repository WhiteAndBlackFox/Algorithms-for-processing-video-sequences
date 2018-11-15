using System;

namespace VideoProcessor.Features.KD
{
    [Serializable]
    public struct KdTreeNodeDistance<T> : IComparable,
        IComparable<KdTreeNodeDistance<T>>, IEquatable<KdTreeNodeDistance<T>>
    {
        private readonly KDTreeNode<T> _node;
        private readonly double _distance;

        /// <summary>
        ///   Gets the node in this pair.
        /// </summary>
        /// 
        public KDTreeNode<T> Node
        {
            get { return _node; }
        }

        /// <summary>
        ///   Gets the distance of the node from the query point.
        /// </summary>
        /// 
        public double Distance
        {
            get { return _distance; }
        }

        /// <summary>
        ///   Creates a new <see cref="KdTreeNodeDistance{T}"/>.
        /// </summary>
        /// 
        /// <param name="node">The node value.</param>
        /// <param name="distance">The distance value.</param>
        /// 
        public KdTreeNodeDistance(KDTreeNode<T> node, double distance)
        {
            this._node = node;
            this._distance = distance;
        }

        /// <summary>
        ///   Determines whether the specified <see cref="System.Object"/>
        ///   is equal to this instance.
        /// </summary>
        /// 
        /// <param name="obj">The <see cref="System.Object"/> to compare
        ///   with this instance.</param>
        /// 
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is 
        ///   equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// 
        public override bool Equals(object obj)
        {
            if (obj is KdTreeNodeDistance<T>)
            {
                var b = (KdTreeNodeDistance<T>)obj;
                return this._node == b._node && this._distance == b._distance;
            }

            return false;
        }

        /// <summary>
        ///   Returns a hash code for this instance.
        /// </summary>
        /// 
        /// <returns>
        ///   A hash code for this instance, suitable for use in hashing
        ///   algorithms and data structures like a hash table. 
        /// </returns>
        /// 
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + _node.GetHashCode();
            hash = hash * 23 + _distance.GetHashCode();
            return hash;
        }

        /// <summary>
        ///   Implements the equality operator.
        /// </summary>
        /// 
        public static bool operator ==(KdTreeNodeDistance<T> a, KdTreeNodeDistance<T> b)
        {
            return a._node == b._node && a._distance == b._distance;
        }

        /// <summary>
        ///   Implements the inequality operator.
        /// </summary>
        /// 
        public static bool operator !=(KdTreeNodeDistance<T> a, KdTreeNodeDistance<T> b)
        {
            return a._node != b._node || a._distance != b._distance;
        }

        /// <summary>
        ///   Implements the lesser than operator.
        /// </summary>
        /// 
        public static bool operator <(KdTreeNodeDistance<T> a, KdTreeNodeDistance<T> b)
        {
            return a._distance < b._distance;
        }

        /// <summary>
        ///   Implements the greater than operator.
        /// </summary>
        /// 
        public static bool operator >(KdTreeNodeDistance<T> a, KdTreeNodeDistance<T> b)
        {
            return a._distance > b._distance;
        }

        /// <summary>
        ///   Determines whether the specified <see cref="KdTreeNodeDistance{T}"/>
        ///   is equal to this instance.
        /// </summary>
        /// 
        /// <param name="other">The <see cref="KdTreeNodeDistance{T}"/> to compare
        ///   with this instance.</param>
        /// 
        /// <returns>
        ///   <c>true</c> if the specified <see cref="KdTreeNodeDistance{T}"/> is 
        ///   equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// 
        public bool Equals(KdTreeNodeDistance<T> other)
        {
            return _distance == other._distance && _node == other._node;
        }

        /// <summary>
        ///   Compares this instance to another node, returning an integer
        ///   indicating whether this instance has a distance that is less
        ///   than, equal to, or greater than the other node's distance.
        /// </summary>
        /// 
        public int CompareTo(KdTreeNodeDistance<T> other)
        {
            return _distance.CompareTo(other._distance);
        }

        /// <summary>
        ///   Compares this instance to another node, returning an integer
        ///   indicating whether this instance has a distance that is less
        ///   than, equal to, or greater than the other node's distance.
        /// </summary>
        /// 
        public int CompareTo(object obj)
        {
            return _distance.CompareTo((KdTreeNodeDistance<T>)obj);
        }
    }
}

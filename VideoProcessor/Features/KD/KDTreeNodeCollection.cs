using System;
using System.Collections.Generic;
using System.Linq;
using AForge;

namespace VideoProcessor.Features.KD
{
    [Serializable]
    public class KdTreeNodeCollection<T> : ICollection<KdTreeNodeDistance<T>>
    {
        readonly SortedSet<double> _distances;
        readonly Dictionary<double, List<KDTreeNode<T>>> _positions;

        DoubleRange _range;
        int _count;


        /// <summary>
        ///   Gets or sets the maximum number of elements on this 
        ///   collection, if specified. A value of zero indicates
        ///   this instance has no upper limit of elements.
        /// </summary>
        /// 
        public int Capacity { get; private set; }

        /// <summary>
        ///   Gets the current distance limits for nodes contained
        ///   at this collection, such as the maximum and minimum
        ///   distances.
        /// </summary>
        /// 
        public DoubleRange Distance
        {
            get { return _range; }
        }

        /// <summary>
        ///   Gets the farthest node in the collection (with greatest distance).
        /// </summary>
        /// 
        public KDTreeNode<T> Farthest
        {
            get { return _positions[_range.Max][0]; }
        }

        /// <summary>
        ///   Gets the nearest node in the collection (with smallest distance).
        /// </summary>
        /// 
        public KDTreeNode<T> Nearest
        {
            get { return _positions[_range.Min][0]; }
        }


        /// <summary>
        ///   Creates a new <see cref="KdTreeNodeCollection{T}"/> with a maximum size.
        /// </summary>
        /// 
        /// <param name="size">The maximum number of elements allowed in this collection.</param>
        /// 
        public KdTreeNodeCollection(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException("size");

            Capacity = size;
            _distances = new SortedSet<double>();
            _positions = new Dictionary<double, List<KDTreeNode<T>>>();
        }

        /// <summary>
        ///   Attempts to add a value to the collection. If the list is full
        ///   and the value is more distant than the farthest node in the
        ///   collection, the value will not be added.
        /// </summary>
        /// 
        /// <param name="value">The node to be added.</param>
        /// <param name="distance">The node distance.</param>
        /// 
        /// <returns>Returns true if the node has been added; false otherwise.</returns>
        /// 
        public bool Add(KDTreeNode<T> value, double distance)
        {
            // The list does have a limit. We have to check if the list
            // is already full or not, to see if we can discard or keep
            // the point

            if (_count < Capacity)
            {
                // The list still has room for new elements. 
                // Just add the value at the right position.

                add(distance, value);

                return true; // a value has been added
            }

            // The list is at its maximum capacity. Check if the value
            // to be added is closer than the current farthest point.

            if (distance < _range.Max)
            {
                // Yes, it is closer. Remove the previous farthest point
                // and insert this new one at an appropriate position to
                // keep the list ordered.

                RemoveFarthest();

                add(distance, value);

                return true; // a value has been added
            }

            // The value is even farther
            return false; // discard it
        }

        /// <summary>
        ///   Attempts to add a value to the collection. If the list is full
        ///   and the value is more distant than the farthest node in the
        ///   collection, the value will not be added.
        /// </summary>
        /// 
        /// <param name="value">The node to be added.</param>
        /// <param name="distance">The node distance.</param>
        /// 
        /// <returns>Returns true if the node has been added; false otherwise.</returns>
        /// 
        public bool AddFarthest(KDTreeNode<T> value, double distance)
        {
            // The list does have a limit. We have to check if the list
            // is already full or not, to see if we can discard or keep
            // the point

            if (_count < Capacity)
            {
                // The list still has room for new elements. 
                // Just add the value at the right position.

                add(distance, value);

                return true; // a value has been added
            }

            // The list is at its maximum capacity. Check if the value
            // to be added is farther than the current nearest point.

            if (distance > _range.Min)
            {
                // Yes, it is farther. Remove the previous nearest point
                // and insert this new one at an appropriate position to
                // keep the list ordered.

                RemoveNearest();

                add(distance, value);

                return true; // a value has been added
            }

            // The value is even closer
            return false; // discard it
        }

        /// <summary>
        ///   Adds the specified item to the collection.
        /// </summary>
        /// 
        /// <param name="distance">The distance from the node to the query point.</param>
        /// <param name="item">The item to be added.</param>
        /// 
        private void add(double distance, KDTreeNode<T> item)
        {
            List<KDTreeNode<T>> position;

            if (!_positions.TryGetValue(distance, out position))
                _positions.Add(distance, position = new List<KDTreeNode<T>>());

            position.Add(item);
            _distances.Add(distance);

            if (_count == 0)
                _range.Max = _range.Min = distance;

            else
            {
                if (distance > _range.Max)
                    _range.Max = distance;
                if (distance < _range.Min)
                    _range.Min = distance;
            }


            _count++;
        }


        /// <summary>
        ///   Removes all elements from this collection.
        /// </summary>
        /// 
        public void Clear()
        {
            _distances.Clear();
            _positions.Clear();

            _count = 0;
            _range.Max = 0;
            _range.Min = 0;
        }


        /// <summary>
        ///   Gets the list of <see cref="KDTreeNode{T}"/> that holds the
        ///   objects laying out at the specified distance, if there is any.
        /// </summary>
        /// 
        public List<KDTreeNode<T>> this[double distance]
        {
            get
            {
                List<KDTreeNode<T>> position;
                if (!_positions.TryGetValue(distance, out position))
                    return null;

                return position;
            }
        }

        /// <summary>
        ///   Gets the <see cref="Accord.MachineLearning.Structures.KDTreeNodeDistance{T}"/>
        ///   at the specified index. Note: this method will iterate over the entire collection
        ///   until the given position is found.
        /// </summary>
        /// 
        public KdTreeNodeDistance<T> this[int index]
        {
            get { return this.ElementAt(index); }
        }

        /// <summary>
        ///   Gets the number of elements in this collection.
        /// </summary>
        /// 
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is read only.
        ///   For this collection, always returns false.
        /// </summary>
        /// 
        /// <value>
        /// 	<c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        /// 
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through this collection.
        /// </summary>
        /// 
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator"/> object 
        ///   that can be used to iterate through the collection.
        /// </returns>
        /// 
        public IEnumerator<KdTreeNodeDistance<T>> GetEnumerator()
        {
            foreach (var position in _positions)
            {
                double distance = position.Key;
                foreach (var node in position.Value)
                    yield return new KdTreeNodeDistance<T>(node, distance);
            }

            yield break;
        }

        /// <summary>
        ///   Returns an enumerator that iterates through this collection.
        /// </summary>
        /// 
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator"/> object that
        ///   can be used to iterate through the collection.
        /// </returns>
        /// 
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _positions.GetEnumerator();
        }




        /// <summary>
        ///   Determines whether this instance contains the specified item.
        /// </summary>
        /// 
        /// <param name="item">The object to locate in the collection. 
        ///   The value can be null for reference types.</param>
        /// 
        /// <returns>
        ///   <c>true</c> if the item is found in the collection; otherwise, <c>false</c>.
        /// </returns>
        /// 
        public bool Contains(KdTreeNodeDistance<T> item)
        {
            List<KDTreeNode<T>> position;
            if (_positions.TryGetValue(item.Distance, out position))
                return position.Contains(item.Node);

            return false;
        }

        /// <summary>
        ///   Copies the entire collection to a compatible one-dimensional <see cref="System.Array"/>, starting
        ///   at the specified <paramref name="arrayIndex">index</paramref> of the <paramref name="array">target
        ///   array</paramref>.
        /// </summary>
        /// 
        /// <param name="array">The one-dimensional <see cref="System.Array"/> that is the destination of the
        ///    elements copied from tree. The <see cref="System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// 
        public void CopyTo(KdTreeNodeDistance<T>[] array, int arrayIndex)
        {
            int index = arrayIndex;

            foreach (var pair in this)
                array[index++] = pair;
        }

        /// <summary>
        ///   Adds the specified item to this collection.
        /// </summary>
        /// 
        /// <param name="item">The item.</param>
        /// 
        public void Add(KdTreeNodeDistance<T> item)
        {
            Add(item.Node, item.Distance);
        }

        /// <summary>
        ///   Removes the first occurrence of a specific object from the collection.
        /// </summary>
        /// 
        /// <param name="item">The object to remove from the collection. 
        /// The value can be null for reference types.</param>
        /// 
        /// <returns>
        ///   <c>true</c> if item is successfully removed; otherwise, <c>false</c>. 
        /// </returns>
        /// 
        public bool Remove(KdTreeNodeDistance<T> item)
        {
            List<KDTreeNode<T>> position;
            if (!_positions.TryGetValue(item.Distance, out position))
                return false;

            if (!position.Remove(item.Node))
                return false;

            _range.Max = _distances.Max;
            _range.Min = _distances.Min;
            _count--;

            return true;
        }

        /// <summary>
        ///   Removes the farthest tree node from this collection.
        /// </summary>
        /// 
        public void RemoveFarthest()
        {
            List<KDTreeNode<T>> position = _positions[_range.Max];

            position.RemoveAt(0);

            if (position.Count() == 0)
            {
                _distances.Remove(_range.Max);
                _range.Max = _distances.Max;
            }

            _count--;
        }

        /// <summary>
        ///   Removes the nearest tree node from this collection.
        /// </summary>
        /// 
        public void RemoveNearest()
        {
            List<KDTreeNode<T>> position = _positions[_range.Min];

            position.RemoveAt(0);

            if (position.Count() == 0)
            {
                _distances.Remove(_range.Min);
                _range.Min = _distances.Min;
            }

            _count--;
        }

    }
}

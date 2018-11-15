using System;
using System.Collections.Generic;

namespace VideoProcessor.Features.KD
{
    public delegate IEnumerator<KDTreeNode<T>> KdTreeTraversalMethod<T>(KdTree<T> tree);

    public class ElementComparer<T> : IComparer<T[]>, IEqualityComparer<T[]>
        where T : IComparable, IEquatable<T>
    {
        public int Index { get; set; }

        public int Compare(T[] x, T[] y)
        {
            return x[Index].CompareTo(y[Index]);
        }
  
        public bool Equals(T[] x, T[] y)
        {
            return x[Index].Equals(y[Index]);
        }

        public int GetHashCode(T[] obj)
        {
            return obj[Index].GetHashCode();
        }
    }

    public class ElementComparer : ElementComparer<double>
    {
    }

    [Serializable]
    public class KdTree<T> : IEnumerable<KDTreeNode<T>>
    {

        private int _count;
        private readonly int _dimensions;
        private readonly int _leaves;

        private KDTreeNode<T> root;
        private Func<double[], double[], double> _distance;


        /// <summary>
        ///   Gets the root of the tree.
        /// </summary>
        /// 
        public KDTreeNode<T> Root
        {
            get { return root; }
        }

        /// <summary>
        ///   Gets the number of dimensions expected
        ///   by the input points of this tree.
        /// </summary>
        /// 
        public int Dimensions
        {
            get { return _dimensions; }
        }

        /// <summary>
        ///   Gets or set the distance function used to
        ///   measure distances amongst points on this tree
        /// </summary>
        /// 
        public Func<double[], double[], double> Distance
        {
            get { return _distance; }
            set { _distance = value; }
        }

        /// <summary>
        ///   Gets the number of elements contained in this
        ///   tree. This is also the number of tree nodes.
        /// </summary>
        /// 
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        ///   Gets the number of leaves contained in this
        ///   tree. This can be used to calibrate approximate
        ///   nearest searchers.
        /// </summary>
        /// 
        public int Leaves
        {
            get { return _leaves; }
        }


        /// <summary>
        ///   Creates a new <see cref="KdTree{T}"/>.
        /// </summary>
        /// 
        /// <param name="dimensions">The number of dimensions in the tree.</param>
        /// 
        public KdTree(int dimensions)
        {
            this._dimensions = dimensions;
            this._distance = Features.Distance.SquareEuclidean;
        }

        /// <summary>
        ///   Creates a new <see cref="KdTree{T}"/>.
        /// </summary>
        /// 
        /// <param name="dimension">The number of dimensions in the tree.</param>
        /// <param name="root">The root node, if already existent.</param>
        /// 
        public KdTree(int dimension, KDTreeNode<T> root)
            : this(dimension)
        {
            this.root = root;

            foreach (var node in this)
            {
                _count++;

                if (node.IsLeaf)
                    _leaves++;
            }
        }

        /// <summary>
        ///   Creates a new <see cref="KdTree{T}"/>.
        /// </summary>
        /// 
        /// <param name="dimension">The number of dimensions in the tree.</param>
        /// <param name="root">The root node, if already existent.</param>
        /// <param name="count">The number of elements in the root node.</param>
        /// <param name="leaves">The number of leaves linked through the root node.</param>
        /// 
        public KdTree(int dimension, KDTreeNode<T> root, int count, int leaves)
            : this(dimension)
        {
            this.root = root;
            this._count = count;
            this._leaves = leaves;
        }


        /// <summary>
        ///   Inserts a value into the tree at the desired position.
        /// </summary>
        /// 
        /// <param name="position">A double-vector with the same number of elements as dimensions in the tree.</param>
        /// <param name="value">The value to be added.</param>
        /// 
        public void Add(double[] position, T value)
        {
            insert(ref root, position, value, 0);
            _count++;
        }

        /// <summary>
        ///   Retrieves the nearest points to a given point within a given radius.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// <param name="radius">The search radius.</param>
        /// <param name="maximum">The maximum number of neighbors to retrieve.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KdTreeNodeCollection<T> Nearest(double[] position, double radius, int maximum)
        {
            var list = new KdTreeNodeCollection<T>(maximum);

            if (root != null)
                nearest(root, position, radius, list);

            return list;
        }

        /// <summary>
        ///   Retrieves the nearest points to a given point within a given radius.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// <param name="radius">The search radius.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KdTreeNodeList<T> Nearest(double[] position, double radius)
        {
            var list = new KdTreeNodeList<T>();

            if (root != null)
                nearest(root, position, radius, list);

            return list;
        }

        /// <summary>
        ///   Retrieves a fixed point of nearest points to a given point.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// <param name="neighbors">The number of neighbors to retrieve.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KdTreeNodeCollection<T> Nearest(double[] position, int neighbors)
        {
            var list = new KdTreeNodeCollection<T>(size: neighbors);

            if (root != null)
                nearest(root, position, list);

            return list;
        }

        /// <summary>
        ///   Retrieves a fixed point of nearest points to a given point.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KDTreeNode<T> Nearest(double[] position)
        {
            double distance;
            return Nearest(position, out distance);
        }

        /// <summary>
        ///   Retrieves a fixed point of nearest points to a given point.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// <param name="distance">The distance from the <paramref name="position"/>
        ///   to its nearest neighbor found in the tree.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KDTreeNode<T> Nearest(double[] position, out double distance)
        {
            KDTreeNode<T> result = root;
            distance = Distance(root.Position, position);

            nearest(root, position, ref result, ref distance);

            return result;
        }

        /// <summary>
        ///   Retrieves a fixed point of nearest points to a given point.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// <param name="neighbors">The number of neighbors to retrieve.</param>
        /// <param name="percentage">The maximum percentage of leaf nodes that
        /// can be visited before the search finishes with an approximate answer.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KdTreeNodeCollection<T> ApproximateNearest(double[] position, int neighbors, double percentage)
        {
            int maxLeaves = (int)(_leaves * percentage);

            var list = new KdTreeNodeCollection<T>(size: neighbors);

            if (root != null)
            {
                int visited = 0;
                approximate(root, position, list, maxLeaves, ref visited);
            }

            return list;
        }

        /// <summary>
        ///   Retrieves a fixed point of nearest points to a given point.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// <param name="percentage">The maximum percentage of leaf nodes that
        /// can be visited before the search finishes with an approximate answer.</param>
        /// <param name="distance">The distance between the query point and its nearest neighbor.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KDTreeNode<T> ApproximateNearest(double[] position, double percentage, out double distance)
        {
            KDTreeNode<T> result = root;
            distance = Distance(root.Position, position);

            int maxLeaves = (int)(_leaves * percentage);

            int visited = 0;
            approximateNearest(root, position, ref result, ref distance, maxLeaves, ref visited);

            return result;
        }

        /// <summary>
        ///   Retrieves a fixed point of nearest points to a given point.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// <param name="percentage">The maximum percentage of leaf nodes that
        /// can be visited before the search finishes with an approximate answer.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KDTreeNode<T> ApproximateNearest(double[] position, double percentage)
        {
            var list = ApproximateNearest(position, neighbors: 1, percentage: percentage);

            return list.Nearest;
        }

        /// <summary>
        ///   Retrieves a fixed point of nearest points to a given point.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// <param name="neighbors">The number of neighbors to retrieve.</param>
        /// <param name="maxLeaves">The maximum number of leaf nodes that can
        /// be visited before the search finishes with an approximate answer.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KdTreeNodeCollection<T> ApproximateNearest(double[] position, int neighbors, int maxLeaves)
        {
            var list = new KdTreeNodeCollection<T>(size: neighbors);

            if (root != null)
            {
                int visited = 0;
                approximate(root, position, list, maxLeaves, ref visited);
            }

            return list;
        }

        /// <summary>
        ///   Retrieves a fixed point of nearest points to a given point.
        /// </summary>
        /// 
        /// <param name="position">The queried point.</param>
        /// <param name="maxLeaves">The maximum number of leaf nodes that can
        /// be visited before the search finishes with an approximate answer.</param>
        /// 
        /// <returns>A list of neighbor points, ordered by distance.</returns>
        /// 
        public KDTreeNode<T> ApproximateNearest(double[] position, int maxLeaves)
        {
            var list = ApproximateNearest(position, neighbors: 1, maxLeaves: maxLeaves);

            return list.Nearest;
        }




        #region internal methods
        /// <summary>
        ///   Creates the root node for a new <see cref="KdTree{T}"/> given
        ///   a set of data points and their respective stored values.
        /// </summary>
        /// 
        /// <param name="points">The data points to be inserted in the tree.</param>
        /// <param name="leaves">Return the number of leaves in the root subtree.</param>
        /// <param name="inPlace">Whether the given <paramref name="points"/> vector
        ///   can be ordered in place. Passing true will change the original order of
        ///   the vector. If set to false, all operations will be performed on an extra
        ///   copy of the vector.</param>
        /// 
        /// <returns>The root node for a new <see cref="KdTree{T}"/>
        ///   contained the given <paramref name="points"/>.</returns>
        /// 
        protected static KDTreeNode<T> CreateRoot(double[][] points, bool inPlace, out int leaves)
        {
            return CreateRoot(points, null, inPlace, out leaves);
        }

        /// <summary>
        ///   Creates the root node for a new <see cref="KdTree{T}"/> given
        ///   a set of data points and their respective stored values.
        /// </summary>
        /// 
        /// <param name="points">The data points to be inserted in the tree.</param>
        /// <param name="values">The values associated with each point.</param>
        /// <param name="leaves">Return the number of leaves in the root subtree.</param>
        /// <param name="inPlace">Whether the given <paramref name="points"/> vector
        ///   can be ordered in place. Passing true will change the original order of
        ///   the vector. If set to false, all operations will be performed on an extra
        ///   copy of the vector.</param>
        /// 
        /// <returns>The root node for a new <see cref="KdTree{T}"/>
        ///   contained the given <paramref name="points"/>.</returns>
        /// 
        protected static KDTreeNode<T> CreateRoot(double[][] points, T[] values, bool inPlace, out int leaves)
        {
            // Initial argument checks for creating the tree
            if (points == null)
                throw new ArgumentNullException("points");

            if (values != null && points.Length != values.Length)
                throw new Exception("values");

            if (!inPlace)
            {
                points = (double[][])points.Clone();

                if (values != null)
                    values = (T[])values.Clone();
            }

            leaves = 0;

            int dimensions = points[0].Length;

            // Create a comparer to compare individual array
            // elements at specified positions when sorting
            ElementComparer comparer = new ElementComparer();

            // Call the recursive algorithm to operate on the whole array (from 0 to points.Length)
            KDTreeNode<T> root = create(points, values, 0, dimensions, 0, points.Length, comparer, ref leaves);

            // Create and return the newly formed tree
            return root;
        }
        #endregion


        #region Recursive methods
        private void nearest(KDTreeNode<T> current, double[] position,
            double radius, ICollection<KdTreeNodeDistance<T>> list)
        {
            // Check if the distance of the point from this
            // node is within the desired radius, and if it
            // is, add to the list of nearest nodes.

            double d = _distance(position, current.Position);

            if (d <= radius)
                list.Add(new KdTreeNodeDistance<T>(current, d));

            // Prepare for recursion. The following null checks
            // will be used to avoid function calls if possible

            double value = position[current.Axis];
            double median = current.Position[current.Axis];

            if (value < median)
            {
                if (current.Left != null)
                    nearest(current.Left, position, radius, list);

                if (current.Right != null)
                    if (Math.Abs(value - median) <= radius)
                        nearest(current.Right, position, radius, list);
            }
            else
            {
                if (current.Right != null)
                    nearest(current.Right, position, radius, list);

                if (current.Left != null)
                    if (Math.Abs(value - median) <= radius)
                        nearest(current.Left, position, radius, list);
            }
        }

        private void nearest(KDTreeNode<T> current, double[] position, KdTreeNodeCollection<T> list)
        {
            // Compute distance from this node to the point
            double d = _distance(position, current.Position);

            if (current.IsLeaf)
            {
                // Base: node is leaf
                list.Add(current, d);
            }
            else
            {
                // Check for leafs on the opposite sides of 
                // the subtrees to nearest possible neighbors.

                // Prepare for recursion. The following null checks
                // will be used to avoid function calls if possible

                double value = position[current.Axis];
                double median = current.Position[current.Axis];

                if (value < median)
                {
                    if (current.Left != null)
                        nearest(current.Left, position, list);

                    list.Add(current, d);

                    if (current.Right != null)
                        if (Math.Abs(value - median) <= list.Distance.Max)
                            nearest(current.Right, position, list);
                }
                else
                {
                    if (current.Right != null)
                        nearest(current.Right, position, list);

                    list.Add(current, d);

                    if (current.Left != null)
                        if (Math.Abs(value - median) <= list.Distance.Max)
                            nearest(current.Left, position, list);
                }
            }
        }

        private void nearest(KDTreeNode<T> current, double[] position, ref KDTreeNode<T> match, ref double minDistance)
        {
            // Compute distance from this node to the point
            double d = _distance(position, current.Position);

            if (current.IsLeaf)
            {
                // Base: node is leaf
                if (d < minDistance)
                {
                    minDistance = d;
                    match = current;
                }
            }
            else
            {
                // Check for leafs on the opposite sides of 
                // the subtrees to nearest possible neighbors.

                // Prepare for recursion. The following null checks
                // will be used to avoid function calls if possible

                double value = position[current.Axis];
                double median = current.Position[current.Axis];

                if (value < median)
                {
                    if (current.Left != null)
                        nearest(current.Left, position, ref match, ref minDistance);

                    if (d < minDistance)
                    {
                        minDistance = d;
                        match = current;
                    }

                    if (current.Right != null)
                        if (Math.Abs(value - median) <= minDistance)
                            nearest(current.Right, position, ref match, ref minDistance);
                }
                else
                {
                    if (current.Right != null)
                        nearest(current.Right, position, ref match, ref minDistance);

                    if (d < minDistance)
                    {
                        minDistance = d;
                        match = current;
                    }

                    if (current.Left != null)
                        if (Math.Abs(value - median) <= minDistance)
                            nearest(current.Left, position, ref match, ref minDistance);
                }
            }
        }


        private bool approximate(KDTreeNode<T> current, double[] position,
            KdTreeNodeCollection<T> list, int maxLeaves, ref int visited)
        {
            // Compute distance from this node to the point
            double d = _distance(position, current.Position);

            if (current.IsLeaf)
            {
                // Base: node is leaf
                list.Add(current, d);

                visited++;

                if (visited > maxLeaves)
                    return true;
            }
            else
            {
                // Check for leafs on the opposite sides of 
                // the subtrees to nearest possible neighbors.

                // Prepare for recursion. The following null checks
                // will be used to avoid function calls if possible

                double value = position[current.Axis];
                double median = current.Position[current.Axis];

                if (value < median)
                {
                    if (current.Left != null)
                        if (approximate(current.Left, position, list, maxLeaves, ref visited))
                            return true;

                    list.Add(current, d);

                    if (current.Right != null)
                        if (Math.Abs(value - median) <= list.Distance.Max)
                            if (approximate(current.Right, position, list, maxLeaves, ref visited))
                                return true;
                }
                else
                {
                    if (current.Right != null)
                        approximate(current.Right, position, list, maxLeaves, ref visited);

                    list.Add(current, d);

                    if (current.Left != null)
                        if (Math.Abs(value - median) <= list.Distance.Max)
                            if (approximate(current.Left, position, list, maxLeaves, ref visited))
                                return true;
                }
            }

            return false;
        }

        private bool approximateNearest(KDTreeNode<T> current, double[] position,
           ref KDTreeNode<T> match, ref double minDistance, int maxLeaves, ref int visited)
        {
            // Compute distance from this node to the point
            double d = _distance(position, current.Position);

            if (current.IsLeaf)
            {
                // Base: node is leaf
                if (d < minDistance)
                {
                    minDistance = d;
                    match = current;
                }

                visited++;

                if (visited > maxLeaves)
                    return true;
            }
            else
            {
                // Check for leafs on the opposite sides of 
                // the subtrees to nearest possible neighbors.

                // Prepare for recursion. The following null checks
                // will be used to avoid function calls if possible

                double value = position[current.Axis];
                double median = current.Position[current.Axis];

                if (value < median)
                {
                    if (current.Left != null)
                        if (approximateNearest(current.Left, position,
                            ref match, ref minDistance, maxLeaves, ref visited))
                            return true;

                    if (d < minDistance)
                    {
                        minDistance = d;
                        match = current;
                    }

                    if (current.Right != null)
                        if (Math.Abs(value - median) <= minDistance)
                            if (approximateNearest(current.Right, position,
                                ref match, ref minDistance, maxLeaves, ref visited))
                                return true;
                }
                else
                {
                    if (current.Right != null)
                        approximateNearest(current.Right, position,
                            ref match, ref minDistance, maxLeaves, ref visited);

                    if (d < minDistance)
                    {
                        minDistance = d;
                        match = current;
                    }

                    if (current.Left != null)
                        if (Math.Abs(value - median) <= minDistance)
                            if (approximateNearest(current.Left, position,
                                ref match, ref minDistance, maxLeaves, ref visited))
                                return true;
                }
            }

            return false;
        }


        private void insert(ref KDTreeNode<T> node, double[] position, T value, int depth)
        {
            if (node == null)
            {
                // Base case: node is null
                node = new KDTreeNode<T>()
                {
                    Axis = depth % _dimensions,
                    Position = position,
                    Value = value
                };
            }
            else
            {
                // Recursive case: keep looking for a position to insert

                if (position[node.Axis] < node.Position[node.Axis])
                {
                    KDTreeNode<T> child = node.Left;
                    insert(ref child, position, value, depth + 1);
                    node.Left = child;
                }
                else
                {
                    KDTreeNode<T> child = node.Right;
                    insert(ref child, position, value, depth + 1);
                    node.Right = child;
                }
            }
        }


        private static KDTreeNode<T> create(double[][] points, T[] values,
           int depth, int k, int start, int length, ElementComparer comparer, ref int leaves)
        {
            if (length <= 0)
                return null;

            // We will be doing sorting in place
            int axis = comparer.Index = depth % k;
            Array.Sort(points, values, start, length, comparer);

            // Middle of the input section
            int half = start + length / 2;

            // Start and end of the left branch
            int leftStart = start;
            int leftLength = half - start;

            // Start and end of the right branch
            int rightStart = half + 1;
            int rightLength = length - length / 2 - 1;

            // The median will be located halfway in the sorted array
            var median = points[half];
            var value = values != null ? values[half] : default(T);

            depth++;

            // Continue with the recursion, passing the appropriate left and right array sections
            KDTreeNode<T> left = create(points, values, depth, k, leftStart, leftLength, comparer, ref leaves);
            KDTreeNode<T> right = create(points, values, depth, k, rightStart, rightLength, comparer, ref leaves);

            if (left == null && right == null)
                leaves++;

            // Backtrack and create
            return new KDTreeNode<T>()
            {
                Axis = axis,
                Position = median,
                Value = value,
                Left = left,
                Right = right,
            };
        }
        #endregion


        /// <summary>
        ///   Removes all nodes from this tree.
        /// </summary>
        /// 
        public void Clear()
        {
            this.root = null;
        }

        /// <summary>
        ///   Copies the entire tree to a compatible one-dimensional <see cref="System.Array"/>, starting
        ///   at the specified <paramref name="arrayIndex">index</paramref> of the <paramref name="array">
        ///   target array</paramref>.
        /// </summary>
        /// 
        /// <param name="array">The one-dimensional <see cref="System.Array"/> that is the destination of the
        ///    elements copied from tree. The <see cref="System.Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// 
        public void CopyTo(KDTreeNode<T>[] array, int arrayIndex)
        {
            foreach (var node in this)
            {
                array[arrayIndex++] = node;
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the tree.
        /// </summary>
        /// 
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator"/> object 
        ///   that can be used to iterate through the collection.
        /// </returns>
        /// 
        public IEnumerator<KDTreeNode<T>> GetEnumerator()
        {
            if (root == null)
                yield break;

            var stack = new Stack<KDTreeNode<T>>(new[] { root });

            while (stack.Count != 0)
            {
                KDTreeNode<T> current = stack.Pop();

                yield return current;

                if (current.Left != null)
                    stack.Push(current.Left);

                if (current.Right != null)
                    stack.Push(current.Right);
            }
        }

        /// <summary>
        ///   Traverse the tree using a <see cref="KDTreeTraversal">tree traversal
        ///   method</see>. Can be iterated with a foreach loop.
        /// </summary>
        /// 
        /// <param name="method">The tree traversal method. Common methods are
        /// available in the <see cref="KDTreeTraversal"/>static class.</param>
        /// 
        /// <returns>An <see cref="IEnumerable{T}"/> object which can be used to
        /// traverse the tree using the chosen traversal method.</returns>
        /// 
        public IEnumerable<KDTreeNode<T>> Traverse(KdTreeTraversalMethod<T> method)
        {
            return new KDTreeTraversal(this, method);
        }


        /// <summary>
        ///   Returns an enumerator that iterates through the tree.
        /// </summary>
        /// 
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// 
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        private class KDTreeTraversal : IEnumerable<KDTreeNode<T>>
        {

            private KdTree<T> tree;
            private KdTreeTraversalMethod<T> method;

            public KDTreeTraversal(KdTree<T> tree, KdTreeTraversalMethod<T> method)
            {
                this.tree = tree;
                this.method = method;
            }

            public IEnumerator<KDTreeNode<T>> GetEnumerator()
            {
                return method(tree);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return method(tree);
            }
        }
    }

    public class KDTree : KdTree<Object>
    {

        /// <summary>
        ///   Creates a new <see cref="KdTree{T}"/>.
        /// </summary>
        /// 
        /// <param name="dimensions">The number of dimensions in the tree.</param>
        /// 
        public KDTree(int dimensions)
            : base(dimensions)
        {
        }

        /// <summary>
        ///   Creates a new <see cref="KdTree{T}"/>.
        /// </summary>
        /// 
        /// <param name="dimension">The number of dimensions in the tree.</param>
        /// <param name="root">The root node, if already existent.</param>
        /// 
        public KDTree(int dimension, KdTreeNode root) 
            : base(dimension, root)
        {
        }

        /// <summary>
        ///   Creates a new <see cref="KdTree{T}"/>.
        /// </summary>
        /// 
        /// <param name="dimension">The number of dimensions in the tree.</param>
        /// <param name="root">The root node, if already existent.</param>
        /// 
        public KDTree(int dimension, KDTreeNode<Object> root)
            : base(dimension, root)
        {
        }

        /// <summary>
        ///   Creates a new <see cref="KdTree{T}"/>.
        /// </summary>
        /// 
        /// <param name="dimension">The number of dimensions in the tree.</param>
        /// <param name="root">The root node, if already existent.</param>
        /// <param name="count">The number of elements in the root node.</param>
        /// <param name="leaves">The number of leaves linked through the root node.</param>
        /// 
        public KDTree(int dimension, KdTreeNode root, int count, int leaves)
            : base(dimension, root, count, leaves)
        {
        }

        /// <summary>
        ///   Creates a new <see cref="KdTree{T}"/>.
        /// </summary>
        /// 
        /// <param name="dimension">The number of dimensions in the tree.</param>
        /// <param name="root">The root node, if already existent.</param>
        /// <param name="count">The number of elements in the root node.</param>
        /// <param name="leaves">The number of leaves linked through the root node.</param>
        /// 
        public KDTree(int dimension, KDTreeNode<Object> root, int count, int leaves)
            : base(dimension, root, count, leaves)
        {
        }


        /// <summary>
        ///   Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of the value to be stored.</typeparam>
        /// 
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="inPlace">Whether the given <paramref name="points"/> vector
        ///   can be ordered in place. Passing true will change the original order of
        ///   the vector. If set to false, all operations will be performed on an extra
        ///   copy of the vector.</param>
        /// 
        /// <returns>A <see cref="KdTree{T}"/> populated with the given data points.</returns>
        /// 
        public static KdTree<T> FromData<T>(double[][] points, bool inPlace = false)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            if (points.Length == 0)
                throw new ArgumentException("Insufficient points for creating a tree.");

            int leaves;

            var root = KdTree<T>.CreateRoot(points, inPlace, out leaves);

            return new KdTree<T>(points[0].Length, root, points.Length, leaves);
        }

        /// <summary>
        ///   Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// 
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="inPlace">Whether the given <paramref name="points"/> vector
        ///   can be ordered in place. Passing true will change the original order of
        ///   the vector. If set to false, all operations will be performed on an extra
        ///   copy of the vector.</param>
        /// 
        /// <returns>A <see cref="KdTree{T}"/> populated with the given data points.</returns>
        /// 
        public static KDTree FromData(double[][] points, bool inPlace = false)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            if (points.Length == 0)
                throw new ArgumentException("Insufficient points for creating a tree.");

            int leaves;

            var root = CreateRoot(points, inPlace, out leaves);

            return new KDTree(points[0].Length, root, points.Length, leaves);
        }

        /// <summary>
        ///   Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of the value to be stored.</typeparam>
        /// 
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="values">The corresponding values at each data point.</param>
        /// <param name="inPlace">Whether the given <paramref name="points"/> vector
        ///   can be ordered in place. Passing true will change the original order of
        ///   the vector. If set to false, all operations will be performed on an extra
        ///   copy of the vector.</param>
        /// 
        /// <returns>A <see cref="KdTree{T}"/> populated with the given data points.</returns>
        /// 
        public static KdTree<T> FromData<T>(double[][] points, T[] values, bool inPlace = false)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            if (points.Length == 0)
                throw new ArgumentException("Insufficient points for creating a tree.");

            int leaves;

            var root = KdTree<T>.CreateRoot(points, values, inPlace, out leaves);
            
            return new KdTree<T>(points[0].Length, root, points.Length, leaves);
        }

        /// <summary>
        ///   Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// 
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="distance">The distance function to use.</param>
        /// <param name="inPlace">Whether the given <paramref name="points"/> vector
        ///   can be ordered in place. Passing true will change the original order of
        ///   the vector. If set to false, all operations will be performed on an extra
        ///   copy of the vector.</param>
        /// 
        /// <returns>A <see cref="KdTree{T}"/> populated with the given data points.</returns>
        /// 
        public static KDTree FromData(double[][] points, Func<double[], double[], double> distance,
            bool inPlace = false)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            if (distance == null)
                throw new ArgumentNullException("distance");

            if (points.Length == 0)
                throw new ArgumentException("Insufficient points for creating a tree.");

            int leaves;

            var root = CreateRoot(points, inPlace, out leaves);

            return new KDTree(points[0].Length, root, points.Length, leaves)
            {
                Distance = distance,
            };
        }

        /// <summary>
        ///   Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of the value to be stored.</typeparam>
        /// 
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="values">The corresponding values at each data point.</param>
        /// <param name="distance">The distance function to use.</param>
        /// <param name="inPlace">Whether the given <paramref name="points"/> vector
        ///   can be ordered in place. Passing true will change the original order of
        ///   the vector. If set to false, all operations will be performed on an extra
        ///   copy of the vector.</param>
        /// 
        /// <returns>A <see cref="KdTree{T}"/> populated with the given data points.</returns>
        /// 
        public static KdTree<T> FromData<T>(double[][] points, T[] values, 
            Func<double[], double[], double> distance, bool inPlace = false)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            if (distance == null)
                throw new ArgumentNullException("distance");

            int leaves;

            var root = KdTree<T>.CreateRoot(points, values, inPlace, out leaves);

            return new KdTree<T>(points[0].Length, root, points.Length, leaves)
            {
                Distance = distance,
            };
        }

        /// <summary>
        ///   Creates a new k-dimensional tree from the given points.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of the value to be stored.</typeparam>
        /// 
        /// <param name="points">The points to be added to the tree.</param>
        /// <param name="distance">The distance function to use.</param>
        /// <param name="inPlace">Whether the given <paramref name="points"/> vector
        ///   can be ordered in place. Passing true will change the original order of
        ///   the vector. If set to false, all operations will be performed on an extra
        ///   copy of the vector.</param>
        /// 
        /// <returns>A <see cref="KdTree{T}"/> populated with the given data points.</returns>
        /// 
        public static KdTree<T> FromData<T>(double[][] points, Func<double[], double[], double> distance, 
            bool inPlace = false)
        {
            if (distance == null)
                throw new ArgumentNullException("distance");

            int leaves;

            var root = KdTree<T>.CreateRoot(points, inPlace, out leaves);

            return new KdTree<T>(points[0].Length, root, points.Length, leaves)
            {
                Distance = distance
            };
        }

    }
}

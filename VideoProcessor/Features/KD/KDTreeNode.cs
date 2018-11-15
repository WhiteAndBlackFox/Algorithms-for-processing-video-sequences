using System;
using System.Text;

namespace VideoProcessor.Features.KD
{
    /// <summary>
    ///   K-dimensional tree node.
    /// </summary>
    /// 
    /// <remarks>
    ///   This class provides a shorthand notation for 
    ///   the actual <see cref="KDTreeNode{T}"/> type.
    /// </remarks>
    /// 
    [Serializable]
    public class KdTreeNode : KDTreeNode<Object>
    {
      
    }

    /// <summary>
    ///   K-dimensional tree node.
    /// </summary>
    /// 
    /// <typeparam name="T">The type of the value being stored.</typeparam>
    /// 
    [Serializable]
    public class KDTreeNode<T>
    {
        /// <summary>
        ///   Gets or sets the position of 
        ///   the node in spatial coordinates.
        /// </summary>
        /// 
        public double[] Position { get; set; }

        /// <summary>
        ///   Gets or sets the dimension index of the split. This value is a
        ///   index of the <see cref="Position"/> vector and as such should
        ///   be higher than zero and less than the number of elements in <see cref="Position"/>.
        /// </summary>
        /// 
        public int Axis { get; set; }

        /// <summary>
        ///   Gets or sets the left subtree of this node.
        /// </summary>
        /// 
        public KDTreeNode<T> Left { get; set; }

        /// <summary>
        ///   Gets or sets the right subtree of this node.
        /// </summary>
        /// 
        public KDTreeNode<T> Right { get; set; }

        /// <summary>
        ///   Gets or sets the value being stored at this node.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        ///   Gets whether this node is a leaf (has no children).
        /// </summary>
        /// 
        public bool IsLeaf
        {
            get { return Left == null && Right == null; }
        }

        /// <summary>
        ///   Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// 
        /// <returns>
        ///   A <see cref="System.String"/> that represents this instance.
        /// </returns>
        /// 
        public override string ToString()
        {
            if (Position == null)
                return "(null)";

            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            for (int i = 0; i < Position.Length; i++)
            {
                sb.Append(Position[i]);
                if (i < Position.Length - 1)
                    sb.Append(",");
            }
            sb.Append(")");

            return sb.ToString();
        }
    }
}

using System;
using System.Linq;
using VideoProcessor.Features.KD;

namespace VideoProcessor.Features.Matching
{

    [Serializable]
    public class KNearestNeighbors<T>
    {
        private int k;

        private T[] inputs;
        private int[] outputs;

        private int classCount;

        private Func<T, T, double> distance;

        private double[] distances;

        public KNearestNeighbors(int k, T[] inputs, int[] outputs, Func<T, T, double> distance)
        {
            checkArgs(k, null, inputs, outputs, distance);

            int classCount = outputs.Distinct().Count();

            initialize(k, classCount, inputs, outputs, distance);
        }

        public KNearestNeighbors(int k, int classes, T[] inputs, int[] outputs, Func<T, T, double> distance)
        {
            checkArgs(k, classes, inputs, outputs, distance);

            initialize(k, classes, inputs, outputs, distance);
        }

        private void initialize(int k, int classes, T[] inputs, int[] outputs, Func<T, T, double> distance)
        {
            this.inputs = inputs;
            this.outputs = outputs;

            this.k = k;
            this.classCount = classes;

            this.distance = distance;
            this.distances = new double[inputs.Length];
        }

        public T[] Inputs
        {
            get { return inputs; }
        }

        public int[] Outputs
        {
            get { return outputs; }
        }

        public int ClassCount
        {
            get { return classCount; }
        }

        public Func<T, T, double> Distance
        {
            get { return distance; }
            set { distance = value; }
        }

        public int K
        {
            get { return k; }
            set
            {
                if (value <= 0 || value > inputs.Length)
                    throw new ArgumentOutOfRangeException("value",
                        "The value for k should be greater than zero and less than total number of input points.");

                k = value;
            }
        }

        public int Compute(T input)
        {
            double[] scores;
            return Compute(input, out scores);
        }

        public int Compute(T input, out double response)
        {
            double[] scores;
            int result = Compute(input, out scores);
            response = scores[result] / scores.Sum();

            return result;
        }

        public virtual int Compute(T input, out double[] scores)
        {
            // Compute all distances
            for (int i = 0; i < inputs.Length; i++)
                distances[i] = distance(input, inputs[i]);

            int[] idx = distances.Bottom(k, inPlace: true);

            scores = new double[classCount];

            for (int i = 0; i < idx.Length; i++)
            {
                int j = idx[i];

                int label = outputs[j];
                double d = distances[i];

                // Convert to similarity measure
                scores[label] += 1.0 / (1.0 + d);
            }

            // Get the maximum weighted score
            int result; scores.Max(out result);

            return result;
        }

        private static void checkArgs(int k, int? classes, T[] inputs, int[] outputs, Func<T, T, double> distance)
        {
            if (k <= 0)
                throw new ArgumentOutOfRangeException("k", "Number of neighbors should be greater than zero.");

            if (classes != null && classes <= 0)
                throw new ArgumentOutOfRangeException("k", "Number of classes should be greater than zero.");

            if (inputs == null)
                throw new ArgumentNullException("inputs");

            if (outputs == null)
                throw new ArgumentNullException("inputs");

            if (inputs.Length != outputs.Length)
                throw new ArgumentOutOfRangeException("outputs",
                    "The number of input vectors should match the number of corresponding output labels");

            if (distance == null)
                throw new ArgumentNullException("distance");
        }
    }
	
	[Serializable]
    public class KNearestNeighbors : KNearestNeighbors<double[]>
    {

        private KdTree<int> tree;

        public KNearestNeighbors(int k, double[][] inputs, int[] outputs)
            : base(k, inputs, outputs, Features.Distance.Euclidean)
        {
            this.tree = KDTree.FromData(inputs, outputs);
        }

        public KNearestNeighbors(int k, int classes, double[][] inputs, int[] outputs)
            : base(k, classes, inputs, outputs, Features.Distance.Euclidean)
        {
            this.tree = KDTree.FromData(inputs, outputs);
        }
		
        public KNearestNeighbors(int k, int classes, double[][] inputs, int[] outputs, Func<double[], double[], double> distance)
            : base(k, classes, inputs, outputs, distance)
        {
            this.tree = KDTree.FromData(inputs, outputs, distance);
        }

        public override int Compute(double[] input, out double[] scores)
        {
            KdTreeNodeCollection<int> neighbors = tree.Nearest(input, this.K);

            scores = new double[ClassCount];

            foreach (var point in neighbors)
            {
                int label = point.Node.Value;
                double d = point.Distance;

                // Convert to similarity measure
                scores[label] += 1.0 / (1.0 + d);
            }

            // Get the maximum weighted score
            int result; scores.Max(out result);

            return result;
        }
    }
}

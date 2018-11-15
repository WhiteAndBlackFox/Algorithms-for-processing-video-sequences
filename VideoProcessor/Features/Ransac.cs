using System;


namespace VideoProcessor.Features
{
    public class Ransac<TModel> where TModel : class
    {
        #region Properties
        public Func<int[], TModel> Fitting { get; set; }

        public Func<int[], bool> Degenerate { get; set; }

        public Func<TModel, double, int[]> Distances { get; set; }

        public double Threshold { get; set; }

        public int Samples { get; set; }

        public int MaxSamplings { get; set; }

        public int MaxEvaluations { get; set; }

        public double Probability { get; set; }
        #endregion

        public Ransac(int minSamples)
        {
            Samples = minSamples;
            MaxSamplings = 100;
            MaxEvaluations = 1000;
            Probability = 0.99;
        }

        public Ransac(int minSamples, double threshold)
        {
            Samples = minSamples;
            MaxSamplings = 100;
            Threshold = threshold;
            MaxEvaluations = 1000;
            Probability = 0.99;
        }

        public Ransac(int minSamples, double threshold, double probability)
        {
            if (minSamples < 0) throw new ArgumentOutOfRangeException("minSamples");
            if (threshold < 0) throw new ArgumentOutOfRangeException("threshold");
            if (probability > 1.0 || probability < 0.0)
                throw new ArgumentException("Probability should be a value between 0 and 1", "probability");

            Samples = minSamples;
            MaxSamplings = 100;
            Threshold = threshold;
            Probability = probability;
            MaxEvaluations = 1000;
        }

        public TModel Compute(int size)
        {
            int[] inliers;
            return Compute(size, out inliers);
        }

        public TModel Compute(int size, out int[] inliers)
        {
            // We are going to find the best model (which fits
            //  the maximum number of inlier points as possible).
            TModel bestModel = null;
            int[] bestInliers = null;
            int maxInliers = 0;

            // For this we are going to search for random samples
            //  of the original points which contains no outliers.

            int count = 0;  // Total number of trials performed
            double N = MaxEvaluations;   // Estimative of number of trials needed.

            // While the number of trials is less than our estimative,
            //   and we have not surpassed the maximum number of trials
            while (count < N && count < MaxEvaluations)
            {
                TModel model = null;
                int samplings = 0;

                // While the number of samples attempted is less
                //   than the maximum limit of attempts
                while (samplings < MaxSamplings)
                {
                    // Select at random s datapoints to form a trial model.
                    var sample = Tools.GetRandom(size, Samples);

                    // If the sampled points are not in a degenerate configuration,
                    if (!Degenerate(sample)) 
                    {
                        // Fit model using the random selection of points
                        model = Fitting(sample);
                        break; // Exit the while loop.
                    }

                    samplings++; // Increase the samplings counter
                }

                // Now, evaluate the distances between total points and the model returning the
                //  indices of the points that are inliers (according to a distance threshold t).
                inliers = Distances(model, Threshold);

                // Check if the model was the model which highest number of inliers:
                if (inliers.Length > maxInliers)
                {
                    // Yes, this model has the highest number of inliers.

                    maxInliers = inliers.Length;  // Set the new maximum,
                    bestModel = model;            // This is the best model found so far,
                    bestInliers = inliers;        // Store the indices of the current inliers.

                    // Update estimate of N, the number of trials to ensure we pick, 
                    //   with probability p, a data set with no outliers.
                    double pInlier = (double)inliers.Length / size;
                    double pNoOutliers = 1.0 - Math.Pow(pInlier, Samples);

                    N = Math.Log(1.0 - Probability) / Math.Log(pNoOutliers);
                }

                count++; // Increase the trial counter.
            }

            inliers = bestInliers;
            return bestModel;
        }
    }
}

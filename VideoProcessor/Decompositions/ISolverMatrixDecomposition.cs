using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using AForge;
using AForge.Imaging.Filters;

namespace VideoProcessor.Features
{
    public interface ISolverMatrixDecomposition<T> where T : struct
    {

        /// <summary>
        ///   Solves a set of equation systems of type <c>A * X = B</c>.
        /// </summary>
        /// 
        T[,] Solve(T[,] value);

        /// <summary>
        ///   Solves a set of equation systems of type <c>A * X = B</c>.
        /// </summary>
        /// 
        T[] Solve(T[] value);

        /// <summary>
        ///   Solves a set of equation systems of type <c>A * X = I</c>.
        /// </summary>
        /// 
        T[,] Inverse();

    }
}

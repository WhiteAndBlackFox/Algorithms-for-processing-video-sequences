using System;
using System.Collections.Generic;

namespace VideoProcessor.Features.KD
{
    [Serializable]
    public class KdTreeNodeList<T> : List<KdTreeNodeDistance<T>>
    {
        public KdTreeNodeList()
        {

        }

        public KdTreeNodeList(int capacity)
            : base(capacity)
        {

        }
    }
}

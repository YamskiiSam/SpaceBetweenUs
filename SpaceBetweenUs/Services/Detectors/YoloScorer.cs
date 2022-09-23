using System;

namespace SpaceBetweenUs.Services.Detectors
{
    internal class YoloScorer<T>
    {
        private string v;

        public YoloScorer(string v)
        {
            this.v = v;
        }

        internal object Predict()
        {
            throw new NotImplementedException();
        }
    }
}
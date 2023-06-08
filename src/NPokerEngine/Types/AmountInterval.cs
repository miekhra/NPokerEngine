using System;

namespace NPokerEngine.Types
{
    public readonly struct AmountInterval
    {
        private readonly float _amount;
        private readonly float? _maxAmount;

        public AmountInterval(float amount, float? maxAmount = null)
        {
            _amount = amount;
            _maxAmount = maxAmount;
        }

        public float Value => _amount;
        public float? MaxValue => _maxAmount;

        public static AmountInterval Empty => new AmountInterval(0);

        public Tuple<float, float?> AsTuple()
            => new Tuple<float, float?>(_amount, _maxAmount);
    }
}

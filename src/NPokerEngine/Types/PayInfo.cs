using System;

namespace NPokerEngine.Types
{
    public class PayInfo : ICloneable
    {
        internal float _amount;
        internal PayInfoStatus _status;

        public PayInfoStatus Status => _status;
        public float Amount => _amount;

        public PayInfo(float amount = 0, PayInfoStatus status = 0)
        {
            _amount = amount;
            _status = status;
        }

        public virtual void UpdateByPay(float amount)
        {
            this._amount += amount;
        }

        public void UpdateToFold()
        {
            this._status = PayInfoStatus.FOLDED;
        }

        public void UpdateToAllin()
        {
            this._status = PayInfoStatus.ALLIN;
        }

        public object Clone()
        {
            return new PayInfo(_amount, _status);
        }
    }
}

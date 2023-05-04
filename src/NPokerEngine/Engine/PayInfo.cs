using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Engine
{
    public class PayInfo
    {
        public const int PAY_TILL_END = 0;
        public const int ALLIN = 1;
        public const int FOLDED = 2;

        internal float _amount;
        internal int _status;

        public int Status => _status;
        public float Amount => _amount;

        public PayInfo(float amount = 0, int status = 0)
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
            this._status = FOLDED;
        }

        public void UpdateToAllin()
        {
            this._status = ALLIN;
        }
    }
}

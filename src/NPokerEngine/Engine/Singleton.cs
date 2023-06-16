using System;
using System.Collections.Generic;
using System.Text;

namespace NPokerEngine.Engine
{
    internal abstract class Singleton<T> where T : new()
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }

                return _instance;
            }
        }
    }
}

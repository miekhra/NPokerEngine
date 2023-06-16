using System;
using System.Collections.Generic;
using System.Text;
using NPokerEngine.Engine;

namespace NPokerEngine.Engine
{
    public static class HandEvaluatorResolver
    {
        private static IHandEvaluator _handEvaluatorInstance = new HandEvaluator();

        public static IHandEvaluator Get() => _handEvaluatorInstance;

        public static void Register(IHandEvaluator handEvaluator) => _handEvaluatorInstance = handEvaluator;

        public static void ResoreDefault() => _handEvaluatorInstance = new HandEvaluator();
    }
}

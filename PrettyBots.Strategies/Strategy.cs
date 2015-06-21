using System;
using PrettyBots.Visitors;

namespace PrettyBots.Strategies
{
    public interface IStrategy
    {
        StrategyContext Context { get; }
    }

    public class Strategy : IStrategy
    {
        public StrategyContext Context { get; private set; }

        public Strategy(StrategyContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            Context = context;
        }
    }
}

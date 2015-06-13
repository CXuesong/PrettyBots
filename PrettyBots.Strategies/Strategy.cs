﻿using System;
using PrettyBots.Visitors;

namespace PrettyBots.Strategies
{
    public interface IStrategy
    {
        IVisitor Visitor { get; }
    }

    public class Strategy<TVisitor> : IStrategy where TVisitor : IVisitor
    {
        public TVisitor Visitor { get; private set; }

        IVisitor IStrategy.Visitor
        {
            get { return Visitor; }
        }

        public Strategy(TVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");
            Visitor = visitor;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Visitors
{
    internal static class Logging
    {
        private static TraceSource source = new TraceSource("PrettyBots.Visitors");

        public static void Enter(object obj, string param = null, [CallerMemberName] string memberName = null)
        {
            source.TraceEvent(TraceEventType.Verbose, 0,
                obj.GetType().Name + "#" + ToString(obj) + "." + memberName + " <| " + param);
        }

        public static void Exit(object obj, object result = null, [CallerMemberName] string memberName = null)
        {
            source.TraceEvent(TraceEventType.Verbose, 0,
                obj.GetType().Name + "#" + ToString(obj) + "." + memberName + " -> " + result);
        }

        public static void Exception(object obj, Exception ex, [CallerMemberName] string memberName = null)
        {
            source.TraceEvent(TraceEventType.Error, 0,
                obj.GetType().Name + "#" + ToString(obj) + "." + memberName + " !> " + ex);
        }

        private static string ToString(object obj)
        {
            var f = obj as ForumVisitor;
            if (f != null) return Convert.ToString(f.Name);
            var t = obj as TopicVisitor;
            if (t != null) return Convert.ToString(t.Id);
            var pb = obj as PostVisitorBase;
            if (pb != null) return pb.Id + "@" + pb.Topic.Id;
            return Convert.ToString(obj.GetHashCode());
        }
    }
}
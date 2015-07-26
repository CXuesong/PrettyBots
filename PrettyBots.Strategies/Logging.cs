using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Strategies
{
    internal static class Logging
    {
        private static TraceSource source = new TraceSource("PrettyBots.Strategies");

        public static void Enter(object obj, string param = null, [CallerMemberName] string memberName = null)
        {
            source.TraceEvent(TraceEventType.Verbose, 10,
                obj.GetType().Name + "#" + ToString(obj) + "." + memberName + " <| " + param);
        }

        public static void Enter<T>(object obj, T param, [CallerMemberName] string memberName = null)
        {
            Enter(obj, Convert.ToString(param), memberName);
        }

        public static void Exit(object obj, string result = null, [CallerMemberName] string memberName = null)
        {
            source.TraceEvent(TraceEventType.Verbose, 11,
                obj.GetType().Name + "#" + ToString(obj) + "." + memberName + " -> " + result);
        }

        public static T Exit<T>(object obj, T result, [CallerMemberName] string memberName = null)
        {
            Exit(obj, Convert.ToString(result), memberName);
            return result;
        }

        public static void TraceEvent(object obj, TraceEventType type, string format, params object[] args)
        {
            source.TraceEvent(type, 0,
                obj.GetType().Name + "#" + ToString(obj) + " : " + string.Format(format, args));
        }

        public static void TraceInfo(object obj, string format, params object[] args)
        {
            TraceEvent(obj, TraceEventType.Information, format, args);
        }

        public static void TraceWarning(object obj, string format, params object[] args)
        {
            TraceEvent(obj, TraceEventType.Warning, format, args);
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

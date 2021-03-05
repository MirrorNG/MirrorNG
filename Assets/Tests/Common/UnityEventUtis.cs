using System.Reflection;
using UnityEngine.Events;

namespace Mirage.Tests
{
    public static class UnityEventUtis
    {

        public static int GetListenerNumber(this UnityEventBase unityEvent)
        {
            FieldInfo field = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            object invokeCallList = field.GetValue(unityEvent);
            PropertyInfo property = invokeCallList.GetType().GetProperty("Count");
            return (int)property.GetValue(invokeCallList);
        }
    }
}
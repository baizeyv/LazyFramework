using System;

namespace Lazy
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ManagerUpdateAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ManagerLateUpdateAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ManagerFixedUpdateAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ManagerGUIAttribute : Attribute { }
}

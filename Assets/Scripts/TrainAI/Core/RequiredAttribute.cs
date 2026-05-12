using System;

namespace TrainAI.Core
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class RequiredAttribute : Attribute { }
}

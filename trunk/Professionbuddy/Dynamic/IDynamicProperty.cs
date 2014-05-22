using System;
using HighVoltz.Composites;

namespace HighVoltz.Dynamic
{
    internal interface IDynamicProperty : ICSharpCode
    {
        /// <summary>
        /// This is the IPBComposite that this propery belongs to. It's set at compile time. 
        /// This version guarantees a public setter
        /// </summary>
        new PBAction AttachedComposite { get; set; }

        string Name { get;  }

        Type ReturnType { get; }

        object Value { get; }
    }
}
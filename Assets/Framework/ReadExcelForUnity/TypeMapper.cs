using System;
using System.Collections.Generic;

public class TypeMapper
{
    /// <summary>
    /// 类型映射，可以在这里增加类型
    /// </summary>
    public readonly static Dictionary<string, System.Type> TYPE_MAPPER = new Dictionary<string, Type>
    {
            //System Type
            { "sbyte",typeof(System.SByte) },
            { "byte",typeof(System.Byte) },

            { "int",typeof(System.Int32) },
            { "long",typeof(System.Int64) },

            { "uint",typeof(System.UInt32) },
            { "ulong",typeof(System.UInt64) },

            { "float",typeof(System.Single) },
            { "double",typeof(System.Double) },

            { "string",typeof(System.String) },

            { "bool",typeof(System.Boolean) },

            //Add Your Type
     };
}

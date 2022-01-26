#region Header

/**
 * IJsonWrapper.cs
 *   Interface that represents a type capable of handling all kinds of JSON
 *   data. This is mainly used when mapping objects through JsonMapper, and
 *   it's implemented by JsonData.
 *
 * The authors disclaim copyright to this source code. For more details, see
 * the COPYING file included with this distribution.
 **/

#endregion


using System.Collections;
using System.Collections.Specialized;


namespace LitJson {
    public enum JsonType {
        None,

        Object,
        Array,
        String,
        Int,
        Long,
        Double,
        Boolean,
        BigNumber
    }

    public interface IJsonWrapper : IList, IOrderedDictionary {
        bool IsArray { get; }
        bool IsBoolean { get; }
        bool IsDouble { get; }
        bool IsInt { get; }
        bool IsLong { get; }
        bool IsObject { get; }
        bool IsString { get; }

        // 新增BigNumber类型
        bool IsBigNumber { get; }

        bool GetBoolean();
        double GetDouble();
        int GetInt();
        JsonType GetJsonType();
        long GetLong();
        string GetString();

        // 新增BigNumber类型
        BigNumber GetBigNumber();
        
        void SetBoolean(bool val);
        void SetDouble(double val);
        void SetInt(int val);
        void SetJsonType(JsonType type);
        void SetLong(long val);
        void SetString(string val);

        // 新增BigNumber类型
        void SetBigNumber(BigNumber number);
        
        string ToJson();
        void ToJson(JsonWriter writer);
    }
}
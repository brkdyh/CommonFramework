using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessageSystem;

public class Class : MonoBehaviour,IMultiMessageHandler
{
    [Header("班级")]
    public string class_name;

    //注册的消息UID --> 处理多条消息UID
    public void initMessageUids(List<string> MessageUids)
    {
        MessageUids.Add("Person_Do_Something");

        MessageUids.Add("Class_Do_Something");
    }

    //子ID<=>方法 映射集合
    public void initHandleMethodMap(Dictionary<string, Dictionary<string, MessageHandleMethod>> HandleMethodMap)
    {
        //uid： Person_Do_Something 下的方法映射
        HandleMethodMap["Person_Do_Something"].Add("Do_Something", personDoSomething);

        //uid： Class_Do_Something 下的方法映射
        HandleMethodMap["Class_Do_Something"].Add("Do_Something", classDoSomething);
    }

    private void Start()
    {
        MessageCore.RegisterHandler(this, getMessageFilter);
    }

    private void OnDestroy()
    {
        MessageCore.UnregisterHandler(this);
    }

    void personDoSomething(params object[] ps)
    {
        string thing = ps[0] as string;
        Debug.Log(transform.name + thing);
    }

    void classDoSomething(params object[] ps)
    {
        string thing = ps[0] as string;
        Debug.Log(transform.name + thing);
    }

    /// <summary>
    /// 消息过滤委托,用于判断该对象是否满足过滤条件
    /// </summary>
    /// <param name="msg_uid">待处理的消息UID</param>
    /// <param name="mark">过滤参数</param>
    /// <returns>返回为真时，根据过滤模式判断是否触发</returns>
    public bool getMessageFilter(string msg_uid, object mark)
    {
        if (msg_uid == "Person_Do_Something")
        {
            string str = mark as string;

            if (str == class_name)
                return true;
        }
        return false;
    }
}

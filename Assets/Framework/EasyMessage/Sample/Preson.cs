using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessageSystem;

public class Preson : MonoBehaviour, IMessageHandler
{
    [Header("姓名")]
    public string person_name;

    [Header("班级")]
    public string class_name;

    //注册的消息UID --> 只处理单一消息UID
    public string getMessageUid => "Person_Do_Something";

    //子ID<=>方法 映射集合
    public void initHandleMethodMap(Dictionary<string, MessageHandleMethod> HandleMethodMap)
    {
        HandleMethodMap.Add("Do_Something", doSomething);
    }

    private void Start()
    {
        MessageCore.RegisterHandler(this, getMessageFilter);
    }

    private void OnDestroy()
    {
        MessageCore.UnregisterHandler(this);
    }

    void doSomething(params object[] ps)
    {
        string thing = ps[0] as string;
        Debug.Log(transform.name + thing);
    }

    //实现的消息过滤器 --> 返回值为false时，该对象被过滤
    public bool getMessageFilter(string msg_uid, object mark)
    {
        if (msg_uid == "Person_Do_Something")
        {
            string str = mark as string;

            if (str == person_name || str == class_name)
                return true;
        }
        return false;
    }
}

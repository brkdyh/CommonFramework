using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessageSystem;
using UnityEngine.AI;

public class Person : MonoBehaviour, IMessageHandler
{
    [Header("姓名")]
    public string person_name;

    [Header("班级")]
    public string class_name;

    public TextMesh name_textMesh;
    public TextMesh class_textMesh;
    //注册的消息UID --> 只处理单一消息UID
    public string getMessageUid => "Person_Do_Something";

    //子ID<=>方法 映射集合
    public void initHandleMethodMap(Dictionary<string, MessageHandleMethod> HandleMethodMap)
    {
        HandleMethodMap.Add("Move", move);
        HandleMethodMap.Add("Reset", reset);
    }

    NavMeshAgent agent;

    Vector3 initPostion;
    protected virtual void Start()
    {
        MessageCore.RegisterHandler(this, getMessageFilter);
        agent = GetComponent<NavMeshAgent>();
        name_textMesh.text = person_name;
        class_textMesh.text = class_name;
        initPostion = transform.position;
    }

    private void OnDestroy()
    {
        MessageCore.UnregisterHandler(this);
    }

    void move(params object[] ps)
    {
        string point_name = ps[0] as string;
        var point = GameObject.Find(point_name);
        agent.enabled = true;
        agent.SetDestination(point.transform.position);
    }

    void reset(params object[] ps)
    {
        agent.enabled = false;
        transform.position = initPostion;
        transform.localEulerAngles = Vector3.zero;
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

            if (str == person_name || str == class_name)
            {
                return true;
            }
        }
        return false;
    }
}

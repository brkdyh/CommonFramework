using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestData : SaveJsonData
{
    public override string GetFileName => "TestData";

    public int test_field1 = 1;
    public string test_field2 = "测试";

    public class test_curtom_data
    {
        public float tc_field = 0.5f;
        public test_curtom_data() {}
        public test_curtom_data(float tc_field)
        {
            this.tc_field = tc_field;
        }
    }

    public List<test_curtom_data> test_field3;

    public override T GetDefault<T>()
    {
        TestData data = new TestData();
        data.test_field3 = new List<test_curtom_data>();
        data.test_field3.Add(new test_curtom_data(1.5f));
        data.test_field3.Add(new test_curtom_data(77.57f));
        return data as T;
    }
}

public class SaveExamlpe : MonoBehaviour
{
    TestData testData;

    private void Awake()
    {
        SaveDataMgr.Initialize(onSaveDataVersionChanged);
    }

    //存档版本号变化回调
    void onSaveDataVersionChanged(string oldVersion, string newVersion)
    {
        Debug.LogFormat("存档版本号变化: {0} => {1}", oldVersion, newVersion);
    }

    private void OnGUI()
    {

        if (GUILayout.Button("加载测试存档"))
        {
            testData = SaveDataMgr.Instance.LoadData<TestData>();
        }

        if (testData != null)
        {
            GUILayout.Label("test_field1:");
            testData.test_field1 = int.Parse(GUILayout.TextField(testData.test_field1.ToString()));
            GUILayout.Label("test_field2:");
            testData.test_field2 = GUILayout.TextField(testData.test_field2);

            GUILayout.Label("test_field3:");
            foreach (var tc in testData.test_field3)
            {
                tc.tc_field = float.Parse(GUILayout.TextField(tc.tc_field.ToString()));
            }


            if (GUILayout.Button("保存测试存档"))
            {
                SaveDataMgr.Instance.SaveData<TestData>();
            }

            if (GUILayout.Button("备份测试存档"))
            {
                if (SaveDataMgr.Instance.BackUp())
                    Debug.Log("备份存档成功");
            }

            if (GUILayout.Button("加载备份存档"))
            {
                if (SaveDataMgr.Instance.LoadBackUp())
                {
                    testData = SaveDataMgr.Instance.LoadData<TestData>();
                }
            }
        }
    }
}

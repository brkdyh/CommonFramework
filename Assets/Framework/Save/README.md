# 存档工具
* 拷贝 *Assets/Framework/Save* 路径下的全部文件到 Unity 工程中。存档工具依赖并内置 ***LitJson*** 
* 点击工具栏 *Tools/Save/Open Save Path* 可以打开存档路径。
* 点击工具栏 *Tools/Save/Clear SaveData*	清空存档文件。
---
## 1.1配置说明
* 配置文件路径为 *../Save/Resources/SaveDataConfig.asset* ,如下图所示:

![配置文件](https://github.com/brkdyh/CommonFramework/tree/main/picture/pic_211.png "存档配置文件")

|配置属性|解释|
|---|---|
|是否加密|仅在编辑器下起效，是否对存档进行加密。发布时默认加密|
|AES加密密钥|当前使用的AES加密密钥，点击随机生成按钮可以更换密钥。<br> ***但使用原有密钥进行加密的存档将无法进行解析。*** |
|历史AES密钥|存储使用过的AES密钥，用于回滚密钥。|
|使用Unity版本号|使用的Unity的版本号作为存档版本号。|
|存档版本号|当前存档的版本号。当存档版本号发生变化时，会触发监听的事件，用于处理存档结构变化引起的潜在问题。|

---

## 1.2存档文件结构创建
* 存档文件类型需继承 ***SaveJsonData*** ,示例如下:
```c#
using System.Collections.Generic;

public class TestData : SaveJsonData
{
    public override string GetFileName => "TestData";	//生成存档文件的名称

		//测试基础类型
    public int test_field1 = 1;
    public string test_field2 = "测试";
		
		//测试自定义类型
    public class test_curtom_data
    {
        public float tc_field = 0.5f;
        public test_curtom_data() {}
        public test_curtom_data(float tc_field)
        {
            this.tc_field = tc_field;
        }
    }
    
		//测试数据结构
    public List<test_curtom_data> test_field3;

		//重载此方法为数据设置初始值
    public override T GetDefault<T>()
    {
        TestData data = new TestData();
        data.test_field3 = new List<test_curtom_data>();
        data.test_field3.Add(new test_curtom_data(1.5f));
        data.test_field3.Add(new test_curtom_data(77.57f));
        return data as T;
    }
}
```
---

## 1.3使用示例
* 使用如下方法进行初始化
```c#
public class SaveDataMgr : MonoSingleton<SaveDataMgr>{
    public static void Initialize(Action<string, string> onSaveVersionChange)
    }
```
* 使用如下方法加载存档
```c#
public class SaveDataMgr : MonoSingleton<SaveDataMgr>{
    public T LoadData<T>()
        where T : SaveJsonData, new()
        }
```

* 使用如下方法保存存档
```c#
public class SaveDataMgr : MonoSingleton<SaveDataMgr>{
    public T SaveData<T>()
        where T : SaveJsonData, new()
        }
```

* 使用如下方法备份存档
```c#
public class SaveDataMgr : MonoSingleton<SaveDataMgr>{
   public bool BackUp()
   }
```
* 使用如下方法替换备用存档
```c#
public class SaveDataMgr : MonoSingleton<SaveDataMgr>{
   public bool LoadBackUp()
   }
```

* 请参考 *../Save/Example* 下的例子使用。

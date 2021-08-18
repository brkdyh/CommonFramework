# 读表工具
##  1.1使用说明
* 一.拷贝 *Assets/Framework/ReadExcelForUnity*  路径下的全部文件到 Unity 工程中。
* 二.按如下格式编辑Excel表格,表格第一行为字段名称，第二行为字段数据类型。

**示例**： 

| Name   | Age  | Money       |
| ------ | ---- | ----------- |
| string | int  | double      |
| 张三   | 20   | 59.56       |
| 李四   | 66   | 995.566     |
| 王五   | 32   | 9874554.221 |

* 三.顶部工具栏 *REFU/Open Window* 打开窗口。
```
1. 选择表格文件。
2. 生成数据类型代码。
3. 设置输出路径。
4. 点击读取配置。
```

---
## 1.2注意事项
| 界面         | 说明                                                         |
| ------------ | :----------------------------------------------------------- |
| 当前选择文件 | 已经选择的表格文件。                                         |
| 当前输出路径 | 读取表格时，对应文件的输出路径。                             |
| 使用命名空间 | 使用表格源文件名称作为命名空间，避免因不同表格文件的Sheet名重复而出现生成类型重复的情况。 |
| 生成数据类型 | 每张Sheet会对应一个数据类型，应尽量避免重复的Sheet名称。<br>同一张表的Sheet名称绝对不能重复。 |
| 读取表格数据 | 读取表中数据，并输出到对应的路径中。<br>在读取表格数据前，应确保已经生成了对应的数据类型。 |

 **Editor下对外接口:**  
```c#
/// <summary>
/// 对外接口，读取表格数据
/// </summary>
/// <param name="excelPath">表格路径</param>
/// <param name="exportPath">输出路径</param>
public static void ReadExcel(string excelPath, string exportPath)
```
 **示例:** 
 ```c#
using UnityEitor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using REFU;
public class Example : IPreprocessBuildWithReport{
	public int callbackOrder => -1;
	public void OnPreprocessBuild(BuildReport report)	{
	REFU.ReadExcel(Application.dataPath+"/A.xlsx",Application.dataPath+"/Resources/");
  }
}
 ```


---
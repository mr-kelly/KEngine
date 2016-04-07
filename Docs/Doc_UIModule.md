UI模块/UIModule
=========================
KEngine的UI模块提供基于ResourceModule的UI编辑、加载、调试功能。遵循约定优于配置的原则，UI的加载没有额外配置。


## 约定优于配置，UI资源与UI脚本严格命名对应

假设有UI资源“Test”, 将会去寻找UI资源Test, 并自动添加脚本KUITest.cs

## UI编辑与打包

- 打开KEngine.UnityProject/Assetes/BundleResources/UI/DemoHome.unity
- 菜单选择 KEngine->UI(UGUI)->Export Current UI，生成UI的AssetBundle

## UI脚本/UI桥接类

参看UGUIBridge.cs

UI模块，对一个UI资源加载完成后，具体的行为是可以通过Bridge类进行自定义桥接的。

默认的UGUIBridge，在UI资源加载完成后，自动寻找同名的对应MonoBehaviour脚本进行添加；
比如UIModule.Instance.LoadWindow("Test"); 将自动寻找名叫Test的UI，并自动执行AddComponent("KUITest")

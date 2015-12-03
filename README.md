# KFramework

Clean, lightweight, a extensible unity framework

An **official fork** from CosmosEngine: https://github.com/mr-kelly/CosmosEngine
CosmosEngine的官方改名、改进版

# 特点 / Features

* 精简的代码
* 具备完整的策划->美术->程序工作流
* 适用于PC/Android/IOS平台开发
* 高性能，无反射
* 资源模块良好扩展性，支持高清版、低清版
* 良好异步风格AssetBundle加载
* 基于约定的、无配置式的UI模块
* 基于编译的Excel配置表，配置表可添加图标、注释、批注


# 整体结构：模块插拔与三大基础模块

KEngine本质上只是一个模块容器(Module Container)，它只为各个模块提供初始化管理。

打个类比，计划开发一个住宅社区，KEngine是一块没有开垦的满是泥巴的地，资源模块(ResourceModule)就是为这块地铺上了水泥；UI模块就是这个社区的会所，配置表模块就是这个社区的物业公司，它们都以水泥地的铺设为前提。接着各式各样的楼房，就是各个不同的自定义模块了。

框架中存在三大基础模块，是默认初始化的：

* 资源模块 / ResourceModule
* UI模块 / UIModule
* 配置表模块 / SettingsModule

AppEngine.Create函数可以传入继承IModule的类来实现模块添加。一个IModule是通过协程来进行初始化的。

-----------------------

# 快速入门DEMO

**Unity打开Assets/KEngine.NGUI.Demo/KEngineNGUIDemo.unity**

------------

# KEngine安装器

KEngine安装器，用于对现有的Unity项目进行安装KEngine的快捷操作，提供一种比Unity Package更有效的导入操作。

## KEngine.Installer操作

* 把KEngine.Installer目录拷贝到Unity工程的Assets目录;
* 从菜单KEngine->KEngine Installer打开安装器界面;
* 点击Select Git Project to Install, 选择KEngine源码目录

## 3种拷贝模式
* Hardlink，默认，方便对源码进行修改，立刻就反应到源码目录;
* SymbolLink， 类似Hardlink
* Copy, 拷贝文件，缺点是对安装后的KEngine代码修改，无法反应到源码目录，git提交不方便

## 2种安装模式

* DLL模式，使用KEngine编译后的DLL，Unity编译后的游戏产品将会产生KEngine.dll，编译更快
* Code模式，使用KEngine源码，方便进行修改源码，断点调试


# 针对美术人员的使用指南

_ResourcesBuild_中依次建好产品化所需的目录，如UI、Effect、Audio目录，资源依序放入。

构建系统写入适当的脚本对各个目录进行分别打包。

# 针对策划人员的使用指南

TODO:Excel的表编辑、编译

# 针对开发人员的使用指南

* [资源模块/ResourceModule](Docs/Doc_ResourceModule.md)
	* [简单资源版本控制/AssetVersionControl](Docs/Doc_AssetVersionControl.md)
* [UI模块/UIModule](Docs/Doc_UIModule.md)
* [配置表模块/SettingModule](Docs/Doc_SettingModule.md)



# UI模块 / UI Module

UI模块，以资源模块为基础，进行UI的编辑、打包、加载。

* 约定优于配置，UI资源与UI脚本严格命名对应
* UI资源Test, 严格对应脚本KUITest.cs

## UI编辑与打包
TODO:

## UI脚本
TODO：

# 配置表模块

![ExcelEdit](Docs/ExcelEdit.png)

* 以Excel为配置表编辑工具, 可为Excel添加表头注释、表格图标
* Excel将被编译成纯文本的Tab表格和C#类，
* 运行端的Tab表格读取，为了性能避开使用Attribute、反射

# 其它模块

* TODO: CosmosTable


# 配置文件/Config

Assets/Resources/KEngineConfig.txt为配置文件，可拖入Excel打开

# Demo
![KEngineDemo](Docs/KEngineDemo.png)

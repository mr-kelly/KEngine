使用KEngine.Installer安装器
============================

KEngine安装器，用于对现有的Unity项目进行安装KEngine的快捷操作，提供一种比Unity Package更有效的导入KEngine操作。

## KEngine.Installer操作

* 把KEngine.Installer目录拷贝到Unity工程的Assets目录;
* 从菜单KEngine->KEngine Installer打开安装器界面;
* 点击Select Git Project to Install, 选择KEngine源码目录

## 3种拷贝模式
* Hardlink，默认，方便对源码进行修改，立刻就到源码目录;
* SymbolLink， 类似Hardlink
* Copy, 拷贝文件，缺点是对安装后的KEngine代码修改，无法反应到源码目录，git提交不方便

## 2种安装模式

* DLL模式，使用KEngine编译后的DLL，Unity编译后的游戏产品将会产生KEngine.dll，编译更快
* Code模式，使用KEngine源码，方便进行修改源码，断点调试

## DLL简介

### 无依赖库 Base Lib DLL

无依赖库指的是, 对Unity引擎无依赖, 它主要由一些基础函数组成. 
部分使用C#作为服务器端的项目, 可以直接使用KEngine提供的函数库, 如配置表读取.

* KEngine.Lib.dll

### 运行时库Runtime DLL


* KEngine.dll
	* ~~KEngine.AssetDep.dll~~
    * KEngine.Tools.dll

### 编辑器库Editor DLL

* KEngine.Editor.dll
	* ~~KEngine.AssetDep.Editor.dll~~

### 不支持DLL的源码

部分代码, 如NGUI, 由于NGUI更多以源码形式存在以适应跨平台, 因此涉及NGUI的代码以源码形式被安装器安装.

* KEngine.AssetDep.NGUI
* KEngine.AssetDep.NGUI.Editor

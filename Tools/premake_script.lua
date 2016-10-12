-- 辅助打印table
function print_r ( t ) 
    local print_r_cache={}
    local function sub_print_r(t,indent)
        if (print_r_cache[tostring(t)]) then
            print(indent.."*"..tostring(t))
        else
            print_r_cache[tostring(t)]=true
            if (type(t)=="table") then
                for pos,val in pairs(t) do
                    if (type(val)=="table") then
                        print(indent.."["..pos.."] => "..tostring(t).." {")
                        sub_print_r(val,indent..string.rep(" ",string.len(pos)+8))
                        print(indent..string.rep(" ",string.len(pos)+6).."}")
                    else
                        print(indent.."["..pos.."] => "..tostring(val))
                    end
                end
            else
                print(indent..tostring(t))
            end
        end
    end
    sub_print_r(t,"  ")
end

-- 扩展，修改project生成xml doc
local function WriteDocumentationFileXml(_premake, _prj, value)
    _premake.w('<DocumentationFile>' .. string.gsub(_prj.buildtarget.relpath, "\.dll", ".xml") .. '</DocumentationFile>')
end

-- Write the current Premake version into our generated files, for reference
premake.override(premake.vstudio.cs2005, "compilerProps", function(base, prj)
    base(prj)
    WriteDocumentationFileXml(premake, prj, XmlDocFileName)
end)

--- Premake5 Dev -----

solution "KEngine.Solution"
    configurations { "Debug", "Release" }
    location ("../Solution/" .. (_ACTION or ""))
    debugdir ("../bin") --PATCHED
    debugargs { } --PATCHED
    defines {"KENGINE_DLL"}

configuration "Debug"
    flags { "Symbols" }
    defines { "_DEBUG", "DEBUG", "TRACE" }
    targetdir "../Build/Debug"
configuration "Release"
    flags { "Optimize" }
    targetdir "../Build/Release"

configuration "vs*"
	defines { "MS_DOTNET" }


local UNITY_ENGINE_DLL = "./UnityEngine.dll"-- "C:/Program Files (x86)/Unity/Editor/Data/Managed/UnityEngine.dll"
local UNITY_UI_DLL = "./UnityEngine.UI.dll" --C:/Program Files (x86)/Unity/Editor/Data/UnityExtensions/Unity/GUISystem/4.6.4/UnityEngine.UI.dll"
local SharpZipLib_DLL = "../KEngine.UnityProject/Assets/KEngine.Tools/SharpZipLib/ICSharpCode.SharpZipLib.dll"
local UNITY_EDITOR_DLL = "./UnityEditor.dll" --C:/Program Files (x86)/Unity/Editor/Data/Managed/UnityEditor.dll"
local IniDll = "../KEngine.UnityProject/Assets/KEngine.Lib/INIFileParser.dll"
local TableMLDLL = "../KEngine.UnityProject/Assets/KEngine.Lib/TableML/TableML.dll"
local TableMLCompilerDLL = "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/TableMLCompiler/TableMLCompiler.dll"

------------------KEngine Base Library --------------------
project "KEngine.Lib"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.Lib/**.cs",
    "../AssemblyInfo.cs",
}

defines
{
}
links
{
    "System",
    IniDll,
    TableMLDLL,
}

------------------KEngine Main--------------------
project "KEngine"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine/**.cs",
    "../AssemblyInfo.cs",
}

defines
{
    "UNITY_5"
}
links
{
    IniDll,
    TableMLDLL,
    "KEngine.Lib",
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
}

------------------KEngine UIModule--------------------
project "KEngine.UI"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.UI/**.cs",
    "../AssemblyInfo.cs",
}

defines
{
    "UNITY_5"
}
links
{
    "KEngine", 
    "KEngine.Lib",
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
}

------------------KEngine UIModule Editor--------------------
project "KEngine.UI.Editor"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.UI.Editor/**.cs",
    "../AssemblyInfo.cs",
}

defines
{
    "UNITY_5"
}
links
{
    "KEngine", 
    "KEngine.Lib",
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
    "KEngine.UI",
    UNITY_EDITOR_DLL,
    "KEngine.Editor",
}

----------------------- KEngine Editor
project "KEngine.Editor"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/**.cs",
    "../KEngine.UnityProject/Assets/KEngine.EditorTools/Editor/**.cs",
    "../AssemblyInfo.cs",
}

defines
{
    "UNITY_5"
}

links
{
    IniDll,
    TableMLDLL,
    TableMLCompilerDLL,
    "KEngine.Lib",
    "KEngine",
    "System",
    UNITY_ENGINE_DLL,
    -- UNITY_UI_DLL,
    UNITY_EDITOR_DLL,
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/CosmosTable.Compiler/DotLiquid.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.OOXML.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.OpenXml4Net.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.OpenXmlFormats.dll",
}


----------------------- KEngine Tools ----------------
project "KEngine.Tools"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.Tools/**.cs",
    "../AssemblyInfo.cs",
}

defines
{
    "UNITY_5"
}

links
{
    "System",
    "KEngine.Lib",
    "KEngine",
    SharpZipLib_DLL,
    UNITY_ENGINE_DLL
}

----------------------- KEngine AssetDep
--[[
project "KEngine.AssetDep"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.AssetDep/**.cs",
    "../AssemblyInfo.cs",
}

defines
{
}

links
{
    "KEngine",
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
}
]]--

----------------------- KEngine AssetDep Editor
--[[
project "KEngine.AssetDep.Editor"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.AssetDep.Editor/Editor/**.cs",
    "../AssemblyInfo.cs",
}

defines
{
}

links
{
    "KEngine.Lib",
    "KEngine",
    "KEngine.AssetDep",
    "KEngine.Editor",
    "KEngine.Tools",
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
    UNITY_EDITOR_DLL,
}
]]--

----------------------- KEngine Test ----------------
project "KEngine.Tests"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.Tests/**.cs",
    "../AssemblyInfo.cs",
}

defines
{
    "NUNIT",
    "UNITY_5",
}

links
{
    "KEngine",
    "KEngine.AssetDep",
    "KEngine.Editor",
    "KEngine.AssetDep.Editor",
    "System",
    "KEngine.Tools",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
    UNITY_EDITOR_DLL,
    "packages/NUnit.3.0.0/lib/net20/nunit.framework.dll",
}

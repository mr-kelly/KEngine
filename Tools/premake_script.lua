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

configuration "Debug"
    flags { "Symbols" }
    defines { "_DEBUG", "DEBUG", "TRACE" }
    targetdir "../Build/Debug"
configuration "Release"
    flags { "Optimize" }
    targetdir "../Build/Release"

configuration "vs*"
	defines { "MS_DOTNET" }

local UNITY_ENGINE_DLL = "C:/Program Files (x86)/Unity/Editor/Data/Managed/UnityEngine.dll"
local UNITY_UI_DLL = "C:/Program Files (x86)/Unity/Editor/Data/UnityExtensions/Unity/GUISystem/4.6.4/UnityEngine.UI.dll"
local SharpZipLib_DLL = "../KEngine.UnityProject/Assets/KEngine/Lib/SharpZipLib/ICSharpCode.SharpZipLib.dll"
local UNITY_EDITOR_DLL = "C:/Program Files (x86)/Unity/Editor/Data/Managed/UnityEditor.dll"

------------------KEngine Main--------------------
project "KEngine"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine/**.cs",
}

defines
{
}
links
{
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
    SharpZipLib_DLL
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
    SharpZipLib_DLL,
    UNITY_EDITOR_DLL,
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/CosmosTable.Compiler/DotLiquid.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.OOXML.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.OpenXml4Net.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.OpenXmlFormats.dll",
}

----------------------- KEngine AssetDep
project "KEngine.AssetDep"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.AssetDep/**.cs",
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

----------------------- KEngine AssetDep Editor
project "KEngine.AssetDep.Editor"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.AssetDep.Editor/Editor/**.cs",
}

defines
{
}

links
{
    "KEngine",
    "KEngine.AssetDep",
    "KEngine.Editor",
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
    UNITY_EDITOR_DLL,
}


----------------------- KEngine Test ----------------
project "KEngine.Tests"
language "C#"
kind "SharedLib"
framework "3.5"

files
{
    "../KEngine.UnityProject/Assets/KEngine.Tests/**.cs",
}

defines
{
    "NUNIT"
}

links
{
    "KEngine",
    "KEngine.AssetDep",
    "KEngine.Editor",
    "KEngine.AssetDep.Editor",
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
    UNITY_EDITOR_DLL,
    "packages/NUnit.3.0.0/lib/net20/nunit.framework.dll",
}

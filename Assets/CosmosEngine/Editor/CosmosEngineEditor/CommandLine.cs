//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                          Version 0.8
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;

public class CommandArgs
{
	public Dictionary<string, string> ArgPairs
	{
		get { return mArgPairs; }
	}

	public List<string> Params
	{
		get { return mParams; }
	}

	List<string> mParams = new List<string>();
	Dictionary<string, string> mArgPairs = new Dictionary<string, string>();
}

public class CommandLine
{
	public static CommandArgs Parse(string[] args)
	{
		char[] kEqual = new char[] { '=' };
		char[] kArgStart = new char[] { '-', '\\' };
		CommandArgs ca = new CommandArgs();

		int ii = -1;
		string token = NextToken(args, ref ii);

		while (token != null)
		{
			if (IsArg(token))
			{
				string arg = token.TrimStart(kArgStart).TrimEnd(kEqual);
				string value = null;

				if (arg.Contains("="))
				{
					string[] r = arg.Split(kEqual, 2);

					if (r.Length == 2 && r[1] != string.Empty)
					{
						arg = r[0];
						value = r[1];
					}
				}

				while (value == null)
				{
					if (ii > args.Length)
						break;

					string next = NextToken(args, ref ii);
					if (next != null)
					{
						if (IsArg(next))
						{
							ii--;
							value = "true";
						}
						else if (next != "=")
						{
							value = next.TrimStart(kEqual);
						}
					}
				}
				ca.ArgPairs.Add(arg, value);
			}
			else if (token != string.Empty)
			{
				ca.Params.Add(token);
			}

			token = NextToken(args, ref ii);
		}
		return ca;
	}

	static bool IsArg(string arg)
	{
		return (arg.StartsWith("-") || arg.StartsWith("\\"));
	}

	static string NextToken(string[] args, ref int ii)
	{
		ii++;
		while (ii < args.Length)
		{
			string cur = args[ii].Trim();
			if (cur != string.Empty)
			{
				return cur;
			}
			ii++;
		}
		return null;
	}
}
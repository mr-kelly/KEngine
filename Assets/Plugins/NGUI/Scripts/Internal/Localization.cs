//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Localization manager is able to parse localization information from text assets.
/// Using it is simple: text = Localization.Get(key), or just add a UILocalize script to your labels.
/// You can switch the language by using Localization.language = "French", for example.
/// This will attempt to load the file called "French.txt" in the Resources folder,
/// or a column "French" from the Localization.csv file in the Resources folder.
/// If going down the TXT language file route, it's expected that the file is full of key = value pairs, like so:
/// 
/// LABEL1 = Hello
/// LABEL2 = Music
/// Info = Localization Example
/// 
/// In the case of the CSV file, the first column should be the "KEY". Other columns
/// should be your localized text values, such as "French" for the first row:
/// 
/// KEY,English,French
/// LABEL1,Hello,Bonjour
/// LABEL2,Music,Musique
/// Info,"Localization Example","Par exemple la localisation"
/// </summary>

public static class Localization
{
	/// <summary>
	/// Whether the localization dictionary has been loaded.
	/// </summary>
 
	static public bool localizationHasBeenSet = false;

	// Loaded languages, if any
	static string[] mLanguages = null;

	// Key = Value dictionary (single language)
	static Dictionary<string, string> mOldDictionary = new Dictionary<string, string>();

	// Key = Values dictionary (multiple languages)
	static Dictionary<string, string[]> mDictionary = new Dictionary<string, string[]>();

	// Index of the selected language within the multi-language dictionary
	static int mLanguageIndex = -1;

	// Currently selected language
	static string mLanguage;

	/// <summary>
	/// Localization dictionary. Dictionary key is the localization key. Dictionary value is the list of localized values (columns in the CSV file).
	/// Be very careful editing this via code, and be sure to set the "KEY" to the list of languages.
	/// </summary>

	static public Dictionary<string, string[]> dictionary
	{
		get
		{
			if (!localizationHasBeenSet) language = PlayerPrefs.GetString("Language", "English");
			return mDictionary;
		}
		set
		{
			localizationHasBeenSet = (value != null);
			mDictionary = value;
		}
	}

	/// <summary>
	/// List of loaded languages. Available if a single Localization.csv file was used.
	/// </summary>

	static public string[] knownLanguages
	{
		get
		{
			if (!localizationHasBeenSet) LoadDictionary(PlayerPrefs.GetString("Language", "English"));
			return mLanguages;
		}
	}

	/// <summary>
	/// Name of the currently active language.
	/// </summary>

	static public string language
	{
		get
		{
			if (string.IsNullOrEmpty(mLanguage))
			{
				string[] lan = knownLanguages;
				mLanguage = PlayerPrefs.GetString("Language", lan != null ? lan[0] : "English");
				LoadAndSelect(mLanguage);
			}
			return mLanguage;
		}
		set
		{
			if (mLanguage != value)
			{
				mLanguage = value;
				LoadAndSelect(value);
			}
		}
	}

	/// <summary>
	/// Load the specified localization dictionary.
	/// </summary>

	static bool LoadDictionary (string value)
	{
		// Try to load the Localization CSV
		TextAsset txt = localizationHasBeenSet ? null : Resources.Load("Localization", typeof(TextAsset)) as TextAsset;
		localizationHasBeenSet = true;

		// Try to load the localization file
		if (txt != null && LoadCSV(txt)) return true;

		// If this point was reached, the localization file was not present
		if (string.IsNullOrEmpty(value)) return false;

		// Not a referenced asset -- try to load it dynamically
		txt = Resources.Load(value, typeof(TextAsset)) as TextAsset;

		if (txt != null)
		{
			Load(txt);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Load the specified language.
	/// </summary>

	static bool LoadAndSelect (string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			if (mDictionary.Count == 0 && !LoadDictionary(value)) return false;
			if (SelectLanguage(value)) return true;
		}

		// Old style dictionary
		if (mOldDictionary.Count > 0) return true;

		// Either the language is null, or it wasn't found
		mOldDictionary.Clear();
		mDictionary.Clear();
		if (string.IsNullOrEmpty(value)) PlayerPrefs.DeleteKey("Language");
		return false;
	}

	/// <summary>
	/// Load the specified asset and activate the localization.
	/// </summary>

	static public void Load (TextAsset asset)
	{
		ByteReader reader = new ByteReader(asset);
		Set(asset.name, reader.ReadDictionary());
	}

	/// <summary>
	/// Load the specified CSV file.
	/// </summary>

	static public bool LoadCSV (TextAsset asset)
	{
		ByteReader reader = new ByteReader(asset);

		// The first line should contain "KEY", followed by languages.
		BetterList<string> temp = reader.ReadCSV();

		// There must be at least two columns in a valid CSV file
		if (temp.size < 2) return false;

		// The first entry must be 'KEY', capitalized
		temp[0] = "KEY";

#if !UNITY_3_5
		// Ensure that the first value is what we expect
		if (!string.Equals(temp[0], "KEY"))
		{
			Debug.LogError("Invalid localization CSV file. The first value is expected to be 'KEY', followed by language columns.\n" +
				"Instead found '" + temp[0] + "'", asset);
			return false;
		}
		else
#endif
		{
			mLanguages = new string[temp.size - 1];
			for (int i = 0; i < mLanguages.Length; ++i)
				mLanguages[i] = temp[i + 1];
		}

		mDictionary.Clear();

		// Read the entire CSV file into memory
		while (temp != null)
		{
			AddCSV(temp);
			temp = reader.ReadCSV();
		}
		return true;
	}

	/// <summary>
	/// Select the specified language from the previously loaded CSV file.
	/// </summary>

	static bool SelectLanguage (string language)
	{
		mLanguageIndex = -1;

		if (mDictionary.Count == 0) return false;

		string[] keys;

		if (mDictionary.TryGetValue("KEY", out keys))
		{
			for (int i = 0; i < keys.Length; ++i)
			{
				if (keys[i] == language)
				{
					mOldDictionary.Clear();
					mLanguageIndex = i;
					mLanguage = language;
					PlayerPrefs.SetString("Language", mLanguage);
					UIRoot.Broadcast("OnLocalize");
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Helper function that adds a single line from a CSV file to the localization list.
	/// </summary>

	static void AddCSV (BetterList<string> values)
	{
		if (values.size < 2) return;
		string[] temp = new string[values.size - 1];
		for (int i = 1; i < values.size; ++i) temp[i - 1] = values[i];
		
		try
		{
			mDictionary.Add(values[0], temp);
		}
		catch (System.Exception ex)
		{
			Debug.LogError("Unable to add '" + values[0] + "' to the Localization dictionary.\n" + ex.Message);
		}
	}

	/// <summary>
	/// Load the specified asset and activate the localization.
	/// </summary>

	static public void Set (string languageName, Dictionary<string, string> dictionary)
	{
		mLanguage = languageName;
		PlayerPrefs.SetString("Language", mLanguage);
		mOldDictionary = dictionary;
		localizationHasBeenSet = false;
		mLanguageIndex = -1;
		mLanguages = new string[] { languageName };
		UIRoot.Broadcast("OnLocalize");
	}

	/// <summary>
	/// Localize the specified value.
	/// </summary>

	static public string Get (string key)
	{
		// Ensure we have a language to work with
		if (!localizationHasBeenSet) language = PlayerPrefs.GetString("Language", "English");

		string val;
		string[] vals;
#if UNITY_IPHONE || UNITY_ANDROID
		string mobKey = key + " Mobile";

		if (mLanguageIndex != -1 && mDictionary.TryGetValue(mobKey, out vals))
		{
			if (mLanguageIndex < vals.Length)
				return vals[mLanguageIndex];
		}
		else if (mOldDictionary.TryGetValue(mobKey, out val)) return val;
#endif
		if (mLanguageIndex != -1 && mDictionary.TryGetValue(key, out vals))
		{
			if (mLanguageIndex < vals.Length)
				return vals[mLanguageIndex];
		}
		else if (mOldDictionary.TryGetValue(key, out val)) return val;

#if UNITY_EDITOR
		Debug.LogWarning("Localization key not found: '" + key + "'");
#endif
		return key;
	}

	/// <summary>
	/// Localize the specified value.
	/// </summary>

	[System.Obsolete("Use Localization.Get instead")]
	static public string Localize (string key) { return Get(key); }

	/// <summary>
	/// Returns whether the specified key is present in the localization dictionary.
	/// </summary>

	static public bool Exists (string key)
	{
		// Ensure we have a language to work with
		if (!localizationHasBeenSet) language = PlayerPrefs.GetString("Language", "English");

#if UNITY_IPHONE || UNITY_ANDROID
		string mobKey = key + " Mobile";
		if (mDictionary.ContainsKey(mobKey)) return true;
		else if (mOldDictionary.ContainsKey(mobKey)) return true;
#endif
		return mDictionary.ContainsKey(key) || mOldDictionary.ContainsKey(key);
	}
}

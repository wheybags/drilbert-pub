using System;
using System.Dynamic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Xna.Framework.Audio;

namespace Drilbert;

public static class GameSettings
{
    static string getSettingsPath()
    {
        return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "settings.json");
    }

    static void saveSettings()
    {
        // dynamic data = new ExpandoObject();
        // data.masterVolume = _masterVolume;
        // data.musicVolume = _musicVolume;

        JsonObject json = new JsonObject
        {
            { "masterVolume", _masterVolume },
            { "musicVolume", _musicVolume },
            { "fullscreen", _fullscreen },
        };

        string path = getSettingsPath();
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(json));
        }
        catch
        {
            Logger.log("Failed to save settings to " + path);
        }
    }

    public static void tryLoadSettings()
    {
        _masterVolume = 1.0f;
        _musicVolume = 1.0f;
        _fullscreen = true;

        string path = getSettingsPath();

        try
        {
            string jsonStr = File.ReadAllText(path);
            JsonObject json = Util.parseJson(jsonStr).AsObject();

            _masterVolume = json["masterVolume"].GetValue<float>();
            _musicVolume = json["musicVolume"].GetValue<float>();
            _fullscreen = json["fullscreen"].GetValue<bool>();
        }
        catch
        {
            Logger.log("Failed reading settings from " + path);
        }
    }

    private static float _masterVolume { get => SoundEffect.MasterVolume; set => SoundEffect.MasterVolume = value; }
    public static float masterVolume { get => _masterVolume; set { _masterVolume = value; saveSettings(); } }

    private static float _musicVolume;
    public static float musicVolume { get => _musicVolume; set { _musicVolume = value; saveSettings(); } }

    private static bool _fullscreen;
    public static bool fullscreen { get => _fullscreen; set { _fullscreen = value; saveSettings(); } }
}
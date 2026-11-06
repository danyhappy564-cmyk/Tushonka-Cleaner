using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Tushonka.Cleaner.Server;

/// <summary>
/// The RAM cleaner's files on this PC, found by walking up from this DLL (SPT_Runtime\user\mods\...) to the folder that
/// holds BepInEx: its config (.cfg: port, on/off, language and every setting), the settings list the game writes at
/// start (settings-catalog.json: names/descriptions in both languages, types, ranges) and the session reports.
/// </summary>
internal sealed class PluginFiles
{
    private const string ConfigName = "com.zzap.tushonka.cleaner.cfg";
    private static readonly object WriteGate = new();

    public string? ConfigPath { get; private set; }
    public string? DataDir { get; private set; }

    public int Port { get; private set; } = 6977;
    public bool Enabled { get; private set; } = true;
    public bool English { get; private set; }

    /// <summary>"section|key" → value as written in the .cfg.</summary>
    public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

    public static PluginFiles Read()
    {
        var files = new PluginFiles();
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        for (var i = 0; i < 6 && dir is not null; i++, dir = Path.GetDirectoryName(dir))
        {
            var bepinex = Path.Combine(dir, "BepInEx");
            if (!Directory.Exists(bepinex))
            {
                continue;
            }

            var config = Path.Combine(bepinex, "config", ConfigName);
            files.ConfigPath = config; // 첫 실행 전에는 없을 수 있습니다. 처음 저장할 때 만듭니다.
            var data = Path.Combine(bepinex, "RamCleaner");
            files.DataDir = Directory.Exists(data) ? data : null;
            break;
        }

        if (files.ConfigPath is not null && File.Exists(files.ConfigPath))
        {
            try
            {
                files.Parse(File.ReadAllLines(files.ConfigPath));
            }
            catch (IOException)
            {
                // being written by the game right now: defaults for this request
            }
        }

        return files;
    }

    private void Parse(string[] lines)
    {
        var section = string.Empty;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1];
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            var eq = line.IndexOf(" =", StringComparison.Ordinal); // "key = value", or "key =" when empty
            if (eq > 0)
            {
                Values[section + "|" + line[..eq]] = line[(eq + 2)..].Trim();
            }
        }

        if (Values.TryGetValue("18. Web page|Port", out var port) && int.TryParse(port, out var p) && p is > 0 and < 65536)
        {
            Port = p;
        }

        Enabled = !(Values.TryGetValue("18. Web page|Enabled", out var enabled) && enabled.Equals("false", StringComparison.OrdinalIgnoreCase));
        English = Values.TryGetValue("0. Mode|Language", out var language) && language == "English";
    }

    /// <summary>The settings list the game wrote at its last start, or null (game never started with 2.12+).</summary>
    /// <summary>
    /// 게임이 시작할 때 저장한 목록이 이 서버 모드와 같은 버전이면 그것을, 아니면 빌드에 들어 있는 목록을 씁니다.
    /// 그래서 설치 직후 게임을 한 번도 켜지 않아도 설정을 고를 수 있습니다.
    /// </summary>
    public JsonElement? Catalog()
    {
        var path = DataDir is null ? null : Path.Combine(DataDir, "settings-catalog.json");
        if (path is not null && File.Exists(path))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("version", out var v) && v.GetString() == new ModMetadata().Version.ToString() &&
                    doc.RootElement.GetProperty("entries").EnumerateArray().All(e => e.TryGetProperty("key", out _)))
                {
                    return doc.RootElement.Clone();
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                // 게임이 쓰는 중이면 아래 목록을 씁니다.
            }
        }

        using var stream = typeof(PluginFiles).Assembly.GetManifestResourceStream("Tushonka.Cleaner.Server.settings-catalog.json");
        if (stream is null)
        {
            return null;
        }

        using var bundled = JsonDocument.Parse(stream);
        return bundled.RootElement.Clone();
    }

    /// <summary>
    /// Checks <paramref name="value"/> against the entry's type/range/choices (clamping numbers like BepInEx does) and
    /// writes it into the .cfg line "key = value" of its section. Returns the stored value, or throws FormatException.
    /// </summary>
    public string Write(JsonElement entry, string value)
    {
        var id = entry.GetProperty("key").GetString()!;
        var cut = id.IndexOf('|');
        return WriteRaw(id[..cut], id[(cut + 1)..], Normalize(entry, value));
    }

    /// <summary>.cfg의 한 줄을 씁니다. 파일이나 줄이 없으면 만듭니다. BepInEx는 이 줄을 읽고, 다음 저장 때 설명을 붙여 정리합니다.</summary>
    public string WriteRaw(string section, string key, string stored)
    {
        if (ConfigPath is null)
        {
            throw new FormatException("config file not found");
        }

        lock (WriteGate)
        {
            var lines = File.Exists(ConfigPath) ? File.ReadAllLines(ConfigPath).ToList() : new List<string>();
            var current = string.Empty;
            var sectionEnd = -1;
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    if (current == section)
                    {
                        sectionEnd = i;
                    }

                    current = line[1..^1];
                    if (current == section)
                    {
                        sectionEnd = lines.Count;
                    }
                }
                else if (current == section && (line == key + " =" || line.StartsWith(key + " = ", StringComparison.Ordinal)))
                {
                    lines[i] = key + " = " + stored;
                    return Save(lines, section, key, stored);
                }
            }

            // 설정 파일에 아직 없는 항목입니다. BepInEx는 이 줄을 읽고, 다음 저장 때 설명을 붙여 정리합니다.
            if (sectionEnd >= 0)
            {
                lines.Insert(sectionEnd, key + " = " + stored);
            }
            else
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("[" + section + "]");
                lines.Add(key + " = " + stored);
            }

            return Save(lines, section, key, stored);
        }
    }

    private string Save(List<string> lines, string section, string key, string stored)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllLines(ConfigPath!, lines, new UTF8Encoding(false)); // same as BepInEx: UTF-8 without BOM
        Values[section + "|" + key] = stored;
        return stored;
    }

    private static string Normalize(JsonElement entry, string value)
    {
        var kind = entry.GetProperty("type").GetString();
        double? min = entry.TryGetProperty("min", out var mn) ? mn.GetDouble() : null;
        double? max = entry.TryGetProperty("max", out var mx) ? mx.GetDouble() : null;
        switch (kind)
        {
            case "bool":
                return bool.TryParse(value, out var b) ? (b ? "true" : "false") : throw new FormatException("expected true/false");
            case "int":
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                {
                    throw new FormatException("expected a whole number");
                }

                return ((int)Math.Clamp(i, min ?? int.MinValue, max ?? int.MaxValue)).ToString(CultureInfo.InvariantCulture);
            case "float":
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) || double.IsNaN(f) || double.IsInfinity(f))
                {
                    throw new FormatException("expected a number");
                }

                return ((float)Math.Clamp(f, min ?? double.MinValue, max ?? double.MaxValue)).ToString(CultureInfo.InvariantCulture);
            case "list":
                return entry.GetProperty("opts").EnumerateArray().Select(o => o.GetString()).FirstOrDefault(o => o == value)
                       ?? throw new FormatException("not one of the choices");
            default:
                return value.Contains('\n') || value.Contains('\r') ? throw new FormatException("one line only") : value.Trim();
        }
    }

    /// <summary>Newest first, at most <paramref name="count"/>.</summary>
    public FileInfo[] Reports(int count) => DataDir is null
        ? []
        : new DirectoryInfo(DataDir).GetFiles("RamCleaner-report-*.html").OrderByDescending(f => f.Name, StringComparer.Ordinal).Take(count).ToArray();
}

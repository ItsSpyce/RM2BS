using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace RM2BS;

public class JSlotObject
{
  [JsonPropertyName("actor")]
  public JSlotActorInfo? ActorInfo { get; set; }

  [JsonPropertyName("bodyMorphs")]
  public required JSlotBodyMorph[] BodyMorphs { get; set; }
}

public class JSlotActorInfo
{
  [JsonPropertyName("hairColor")]
  public required decimal HairColor { get; set; }

  [JsonPropertyName("headTexture")]
  public required string HeadTexture { get; set; }

  [JsonPropertyName("weight")]
  public required decimal Weight { get; set; }
}

public class JSlotBodyMorph
{
  [JsonPropertyName("name")]
  public required string Name { get; set; }

  [JsonPropertyName("keys")]
  public required JSlotBodyMorphKey[] Keys { get; set; }
}

public class JSlotBodyMorphKey
{
  [JsonPropertyName("key")]
  public required string Key { get; set; }

  [JsonPropertyName("value")]
  public required decimal Value { get; set; }
}

[Serializable]
[XmlRoot("SliderPresets")]
public class RaceMenuSliderPresets
{
  [XmlElement("Preset")]
  public RaceMenuSliderPreset[] Presets { get; set; } = [];
}

[Serializable]
public class RaceMenuSliderPreset
{
  [XmlAttribute("name")]
  public required string Name { get; set; }

  [XmlAttribute("set")]
  public required string Set { get; set; }

  [XmlElement("Group")]
  public RaceMenuSliderPresetGroup[] Groups { get; set; } = [];

  [XmlElement("SetSlider")]
  public RaceMenuSliderPresetSlider[] Sliders { get; set; } = [];
}

[Serializable]
public class RaceMenuSliderPresetGroup
{
  [XmlAttribute("name")]
  public required string Name { get; set; }
}

[Serializable]
public class RaceMenuSliderPresetSlider
{
  [XmlAttribute("name")]
  public required string Name { get; set; }

  [XmlAttribute("size")]
  public required string Size { get; set; }

  [XmlAttribute("value")]
  public required decimal Value { get; set; }
}

class Program
{
  public const string SizeSmall = "small";
  public const string SizeBig = "big";

  public static ValueTask<JSlotObject?> ReadJSlotFile(string filename)
  {
    var fullPath = Path.Join(Directory.GetCurrentDirectory(), filename);
    if (!File.Exists(fullPath))
    {
      return ValueTask.FromResult<JSlotObject?>(null);
    }
    return JsonSerializer.DeserializeAsync<JSlotObject>(File.OpenRead(fullPath));
  }

  public static async Task<int> Main()
  {
    var inputDir = Path.Join("SKSE", "Plugins", "CharGen", "Presets");
    var outputDir = Path.Join("CalienteTools", "BodySlide", "SliderPresets");
    if (!Directory.Exists(outputDir))
    {
      Directory.CreateDirectory(outputDir);
    }
    var serializer = new XmlSerializer(typeof(RaceMenuSliderPresets));
    foreach (var file in Directory.EnumerateFiles("SKSE/Plugins/CharGen/Presets", "*.jslot"))
    {
      var filenameWithoutExt = Path.GetFileNameWithoutExtension(file);
      var outputXml = Path.Join(outputDir, $"{filenameWithoutExt}.xml");
      if (File.Exists(outputXml))
      {
        Console.WriteLine($"Found preset for {filenameWithoutExt}, skipping");
        continue;
      }
      var jslotData = await ReadJSlotFile(file);
      if (jslotData is null)
      {
        Console.WriteLine($"Invalid data found at {filenameWithoutExt}, skipping");
        continue;
      }
      var raceMenuSliderPresets = new RaceMenuSliderPresets
      {
        Presets =
        [
          new()
          {
            Name = $"RaceMenu - {filenameWithoutExt}",
            Set = "RaceMenu",
            Groups =
            [
              new() { Name = "3BA" },
              new() { Name = "3BBB" },
              new() { Name = "BHUNP 3BBB" },
              new() { Name = "CBBE bodies" },
              new() { Name = "COCO CBBE 3BBB" },
              new() { Name = "COCO UUNP 3BBB" },
            ],
            Sliders = [.. jslotData.BodyMorphs.Aggregate(new List<RaceMenuSliderPresetSlider>(), (acc, morph) =>
            {
              var key = morph.Keys.FirstOrDefault();
              if (key is null or not { Key: "RaceMenuMorphsCBBE.esp" })
              {
                return acc;
              }
              acc.AddRange(
              [
                new() { Name = morph.Name, Size = SizeSmall, Value = key.Value * 100 },
                new() { Name = morph.Name, Size = SizeBig, Value = key.Value * 100 },
              ]);
              return acc;
            })]
          }
        ]
      };

      // write to output
      using var xmlWriter = File.OpenWrite(outputXml);
      serializer.Serialize(xmlWriter, raceMenuSliderPresets);
      Console.WriteLine($"Created preset {outputXml}");
    }

    return 0;
  }
}
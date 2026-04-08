using System.IO;
using DogDays.Game.Core;
using DogDays.Game.Data;

namespace DogDays.Tests.Unit;

public class QuestDefinitionLoaderTests
{
    [Fact]
    public void LoadFromFile__ParsesQuestDefinitions__FromJson()
    {
        const string json = """
        [
          {
            "id": "meet-grandpa",
            "title": "Meet Grandpa",
            "description": "Say hello outside the cabin.",
            "autoStart": true,
            "objectives": [
              {
                "id": "talk-to-grandpa",
                "description": "Talk to Grandpa.",
                "completion": {
                  "eventType": "NpcTalkedTo",
                  "targetId": "grandpa",
                  "requiredCount": 1
                }
              }
            ]
          }
        ]
        """;

        var filePath = WriteTempQuestFile(json);

        try
        {
            var definitions = QuestDefinitionLoader.LoadFromFile(filePath);

            Assert.Single(definitions);
            Assert.Equal("meet-grandpa", definitions[0].Id);
            Assert.True(definitions[0].AutoStart);
            Assert.Equal(GameEventType.NpcTalkedTo, definitions[0].Objectives[0].Completion.EventType);
            Assert.Equal("grandpa", definitions[0].Objectives[0].Completion.TargetId);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void LoadFromFile__Throws__WhenQuestIdsAreDuplicated()
    {
        const string json = """
        [
          {
            "id": "duplicate",
            "title": "First",
            "description": "First copy.",
            "objectives": [
              {
                "id": "a",
                "description": "A.",
                "completion": {
                  "eventType": "NpcTalkedTo",
                  "requiredCount": 1
                }
              }
            ]
          },
          {
            "id": "duplicate",
            "title": "Second",
            "description": "Second copy.",
            "objectives": [
              {
                "id": "b",
                "description": "B.",
                "completion": {
                  "eventType": "ZoneEntered",
                  "requiredCount": 1
                }
              }
            ]
          }
        ]
        """;

        var filePath = WriteTempQuestFile(json);

        try
        {
            Assert.Throws<InvalidDataException>(() => QuestDefinitionLoader.LoadFromFile(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void LoadFromFile__Throws__WhenRequiredCountIsZero()
    {
        const string json = """
        [
          {
            "id": "broken-quest",
            "title": "Broken Quest",
            "description": "This should fail validation.",
            "objectives": [
              {
                "id": "bad-objective",
                "description": "Impossible objective.",
                "completion": {
                  "eventType": "EnemyKilled",
                  "requiredCount": 0
                }
              }
            ]
          }
        ]
        """;

        var filePath = WriteTempQuestFile(json);

        try
        {
            Assert.Throws<InvalidDataException>(() => QuestDefinitionLoader.LoadFromFile(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string WriteTempQuestFile(string json)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, json);
        return filePath;
    }
}
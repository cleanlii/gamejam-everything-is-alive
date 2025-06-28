using System;
using System.Collections.Generic;
using cfg.level;
using cfg.world;
using cfg.world.map;
using I2.Loc;
using PackageGame.Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class IntIntDicitonary : SerializableDictionary<int, int>
{
}

[Serializable]
public class PropShapeDictionary : SerializableDictionary<Vector2Int, ItemTag>
{
}

[Serializable]
public class ActivateTagDictionary : SerializableDictionary<ItemTag, bool>
{
}

[Serializable]
public class GameObjectListStorage : SerializableDictionary.Storage<List<GameObject>>
{
}

[Serializable]
public class StringListStorage : SerializableDictionary.Storage<List<string>>
{
}

[Serializable]
public class LocalizedStringListStorage : SerializableDictionary.Storage<List<LocalizedString>>
{
}

[Serializable]
public class ItemTagUIElementDictionary : SerializableDictionary<ItemTag, GameObjectListStorage>
{
}

[Serializable]
public class BuffUIElementDictionary : SerializableDictionary<BuffEffect, GameObjectListStorage>
{
}

[Serializable]
public class BuildingIconDictionary : SerializableDictionary<BuildingType, Sprite>
{
}

[Serializable]
public class StateIconDictionary : SerializableDictionary<ObjectState, Sprite>
{
}

[Serializable]
public class SeasonRewardDictionary : SerializableDictionary<Season, int>
{
}

[Serializable]
public class SeasonConnectionDictionary : SerializableDictionary<Season, IntIntDicitonary>
{
}

[Serializable]
public class PackProbabilityDictionary : SerializableDictionary<PipeRank, float>
{
}

[Serializable]
public class PipeProbabilityDictionary : SerializableDictionary<Direction, float>
{
}

[Serializable]
public class TownInfoPrefabDictionary : SerializableDictionary<Season, GameObject>
{
}

[Serializable]
public class TownDetailSpriteDictionary : SerializableDictionary<Season, Sprite>
{
}

[Serializable]
public class LevelDiffNameDictionary : SerializableDictionary<LevelDifficulty, string>
{
}

[Serializable]
public class DiffUIElementDictionary : SerializableDictionary<LevelDifficulty, GameObjectListStorage>
{
}

[Serializable]
public class LevelTagUIElementDictionary : SerializableDictionary<LevelTag, GameObjectListStorage>
{
}

[Serializable]
public class LevelScoreUIElementDictionary : SerializableDictionary<LevelDifficulty, TextMeshProUGUI>
{
}

[Serializable]
public class DiffButtonUIElementDictionary : SerializableDictionary<LevelDifficulty, Button>
{
}

[Serializable]
public class ButtonIdUIElementDictionary : SerializableDictionary<Button, int>
{
}

[Serializable]
public class ButtonListStorage : SerializableDictionary.Storage<List<Button>>
{
}

[Serializable]
public class SeasonButtonListUIElementDictionary : SerializableDictionary<Season, ButtonListStorage>
{
}

[Serializable]
public class SeasonPanelUIElementDictionary : SerializableDictionary<Season, GameObject>
{
}

[Serializable]
public class ButtonInnerImgUIElementDictionary : SerializableDictionary<Button, Image>
{
}
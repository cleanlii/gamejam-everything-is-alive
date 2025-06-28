using UnityEditor;

[CustomPropertyDrawer(typeof(IntIntDicitonary))]
[CustomPropertyDrawer(typeof(PropShapeDictionary))]
[CustomPropertyDrawer(typeof(ActivateTagDictionary))]
[CustomPropertyDrawer(typeof(ItemTagUIElementDictionary))]
[CustomPropertyDrawer(typeof(LevelTagUIElementDictionary))]
[CustomPropertyDrawer(typeof(BuffUIElementDictionary))]
[CustomPropertyDrawer(typeof(BuildingIconDictionary))]
[CustomPropertyDrawer(typeof(StateIconDictionary))]
[CustomPropertyDrawer(typeof(SeasonRewardDictionary))]
[CustomPropertyDrawer(typeof(SeasonConnectionDictionary))]
[CustomPropertyDrawer(typeof(PackProbabilityDictionary))]
[CustomPropertyDrawer(typeof(PipeProbabilityDictionary))]
[CustomPropertyDrawer(typeof(TownInfoPrefabDictionary))]
[CustomPropertyDrawer(typeof(TownDetailSpriteDictionary))]
[CustomPropertyDrawer(typeof(LevelDiffNameDictionary))]
[CustomPropertyDrawer(typeof(DiffUIElementDictionary))]
[CustomPropertyDrawer(typeof(LevelScoreUIElementDictionary))]
[CustomPropertyDrawer(typeof(DiffButtonUIElementDictionary))]
[CustomPropertyDrawer(typeof(ButtonIdUIElementDictionary))]
[CustomPropertyDrawer(typeof(SeasonButtonListUIElementDictionary))]
[CustomPropertyDrawer(typeof(SeasonPanelUIElementDictionary))]
[CustomPropertyDrawer(typeof(ButtonInnerImgUIElementDictionary))]
public class AnySerializableDictionaryPropertyDrawer : SerializableDictionaryPropertyDrawer
{
}
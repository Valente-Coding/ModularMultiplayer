using UnityEngine;

[CreateAssetMenu(fileName = "OC_", menuName = "ModularMultiplayer/SO ObjectConversion")]
public class SO_ObjectConversion : ScriptableObject
{
    public string ConversionName = "New Conversion";
    public GameObject InputPrefab;
    public GameObject OutputPrefab;
    public int ConversionTimeInSeconds = 1;
}

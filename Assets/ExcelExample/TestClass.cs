using System.Collections.Generic;
using DefaultNamespace;
using Lazy;
using UnityEngine;

[ExcelAsset]
public class TestClass : ScriptableObject
{
    public List<MyTest> abcs; // Replace 'EntityType' to an actual type that is serializable.
}

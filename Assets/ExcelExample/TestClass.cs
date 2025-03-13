using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using Lazy.Excel;
using UnityEngine;

[ExcelAsset]
public class TestClass : ScriptableObject
{
    public List<MyTest> abcs; // Replace 'EntityType' to an actual type that is serializable.
}

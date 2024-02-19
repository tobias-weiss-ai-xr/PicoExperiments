using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class AiAgentTest
{
    [SetUp]
    public void Setup()
    {
        EditorSceneManager.OpenScene("Assets\\Scenes\\AiAgent.unity");
    }
    [Test]
    public void VerifyScene()
    {
        Assert.IsTrue(GameObject.Find("Realtime Instance").transform != null);
        Assert.IsTrue(GameObject.Find("doors 1 agent").transform != null);
        // Assert.IsTrue(GameObject.Find("FTManager").GetComponent<FileWriter>() != null);
        Assert.IsTrue(GameObject.Find("XR Origin").transform != null);
        Assert.That(GameObject.Find("EyeTracking"), Is.Not.Null);
    }

    [TearDown]
    public void Teardown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
    }
}
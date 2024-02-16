using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class ShowroomTestScript
{
    [SetUp]
    public void Setup()
    {
        EditorSceneManager.OpenScene("Assets\\Scenes\\Showroom.unity");
    }
    [Test]
    public void VerifyScene()
    {
        Assert.IsTrue(GameObject.Find("Realtime Instance").transform != null);
        Assert.IsTrue(GameObject.Find("doors 1 agent").transform != null);
        // Assert.IsTrue(GameObject.Find("FTManager").GetComponent<FileWriter>() != null);
        var gameObject = GameObject.Find("EyeTracking");
        Assert.That(gameObject, Is.Not.Null);
    }

    [Test]
    public void XrOriginBoxColliderTest()
    {
        Assert.IsTrue(GameObject.Find("XR Origin").GetComponent<BoxCollider>() != null);
    }

    [TearDown]
    public void Teardown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
    }
}
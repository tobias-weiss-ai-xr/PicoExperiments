using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ShowroomTestScript
{
    // A Test behaves as an ordinary method
    [Test]
    public void BasicObjectExistenceTest()
    {
        Assert.IsTrue(GameObject.Find("Realtime Instance").transform != null);
        // Assert.IsTrue(GameObject.Find("FTManager").GetComponent<FileWriter>() != null);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}

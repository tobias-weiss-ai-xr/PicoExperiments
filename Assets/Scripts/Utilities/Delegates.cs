using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Delegates : MonoBehaviour
{
    [SerializeField] private ActionOnTimer actionOnTimer;

    public delegate void TestDelegate();
    public delegate bool TestBoolDelegate(int i);

    private TestDelegate testDelegateFunction;
    private TestBoolDelegate testBoolDelegateFunction;

    private Action testAction;
    private Func<int> testFunc;
    private Func<int, bool> testBoolFunc;

    void Start()
    {
        testDelegateFunction = MyTestDelegateFunction;
        testDelegateFunction += MySecondTestDelegateFunction;
        testDelegateFunction();

        testDelegateFunction = () => { Debug.Log("Lambda expression"); };
        testDelegateFunction();

        testBoolDelegateFunction = (int i) => i > 5;
        Debug.Log(testBoolDelegateFunction(3));

        testAction = () => { Debug.Log("testAction"); };
        testAction();

        testFunc = () => { return 0; };
        Debug.Log(testFunc());

        testBoolFunc = (int i) => i > 5;
        Debug.Log(testBoolFunc(8));

        actionOnTimer.SetTimer(1f, () => { Debug.Log("Timer complete"); });
    }

    private void MyTestDelegateFunction()
    {
        Debug.Log("MyTestDelegateFunction");
    }

    private void MySecondTestDelegateFunction()
    {
        Debug.Log("MySecondTestDelegateFunction");
    }

}

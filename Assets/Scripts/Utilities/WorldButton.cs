using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class WorldButton : XRBaseInteractable
{
    public bool interactable = true;
    public float epsilonInPosition = 0.01f;
    private float yMin = 0, yMax = 0;
    private float clickGuard = 0;
    private float previousHandHeight = 0.0f;
    XRBaseInteractor m_Interactor = null;


    public UnityEvent OnPress = null;
    private bool previousPress = false;

    protected override void Awake()
    {
        base.Awake();
        hoverEntered.AddListener(StartPress);
        //hoverEntered.AddListener(DebugPrint);
        hoverExited.AddListener(EndPress);
        //hoverExited.AddListener(DebugPrint);
    }

    protected override void OnDestroy()
    {
        hoverEntered.RemoveAllListeners();
        hoverExited.RemoveAllListeners();
    }

    private void Start()
    {
        SetMinMax();
    }
    private void Update()
    {
        if(clickGuard > 0f)
            clickGuard -= Time.deltaTime;    
    }

    private void StartPress(BaseInteractionEventArgs args)
    {
        m_Interactor = (XRBaseInteractor)args.interactorObject;
        previousHandHeight = GetLocalYPosition(m_Interactor.transform.parent.position);
    }

    private void EndPress(BaseInteractionEventArgs args)
    {
        m_Interactor = null;
        previousHandHeight = 0.0f;
        previousPress = false;
        SetYPosition(yMax);
    }

    private void SetMinMax()
    {
        Collider collider = GetComponent<Collider>();
        yMin = transform.localPosition.y - (collider.bounds.size.y * 0.5f);
        yMax = transform.localPosition.y;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        if (interactable && m_Interactor)
        {
            float newHandHeight = GetLocalYPosition(m_Interactor.transform.position);
            float handDIfference = previousHandHeight - newHandHeight;
            previousHandHeight = newHandHeight;

            if (handDIfference > 0)
            {
                float newPosition = transform.localPosition.y - handDIfference;
                SetYPosition(newPosition);
                CheckPress();
            }

        }
    }

    private float GetLocalYPosition(Vector3 position)
    {
        Vector3 localPosition = transform.root.InverseTransformPoint(position);
        return localPosition.y;
    }

    private void SetYPosition(float position)
    {
        Vector3 newPosition = transform.localPosition;
        newPosition.y = Mathf.Clamp(position, yMin, yMax);
        transform.localPosition = newPosition;
    }

    private void CheckPress()
    {
        bool inPosition = InPosition();

        if (inPosition && inPosition != previousPress && clickGuard <= 0f)
        {
            OnPress.Invoke();
            clickGuard = 30f;
            DebugPrint();
        }
        previousPress = inPosition;
    }

    private bool InPosition()
    {
        float inRange = Mathf.Clamp(transform.localPosition.y, yMin, yMin + epsilonInPosition);
        return transform.localPosition.y == inRange; // In epsilon range?
    }

    private void DebugPrint() =>
        Debug.Log($"Button Pressed : {transform.name}. Interactor : {m_Interactor.transform.parent.name}.");
}

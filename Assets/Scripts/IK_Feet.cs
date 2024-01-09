using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IK_Feet : MonoBehaviour
{
    public Transform body;
    public IK_Feet otherFoot;
    public bool autoAdjustMovementParameters = true;
    public float speed = 5, stepDistance = .3f, stepLength = .3f, stepHeight = .3f;
    public Vector3 footPosOffset, footRotOffset;

    private float _footSpacing, _stepProgressLerp;
    private Vector3 _oldPos, _currentPos, _nextPos;
    private Vector3 _oldNormal, _currentNormal, _nextNormal;
    public float _velocity;
    private Vector3 _lastBodyPosition;
    // Start is called before the first frame update
    void Start()
    {
        _footSpacing = transform.localPosition.x;
        _currentPos = _nextPos = _oldPos = transform.position;
        _currentNormal = _nextNormal = _oldNormal = transform.up;
        _stepProgressLerp = 1;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = _currentPos + footPosOffset;
        transform.rotation = Quaternion.LookRotation(_currentNormal, body.forward) *Quaternion.Euler(footRotOffset);
        if (autoAdjustMovementParameters)
        {

            _velocity = Mathf.Lerp(_velocity, (body.position - _lastBodyPosition).magnitude / Time.deltaTime, 0.5f);
            _lastBodyPosition = body.position;
            float drf = 10f;
            stepDistance = Mathf.Clamp(Mathf.Lerp(stepDistance, Mathf.Sqrt(_velocity* drf) / drf, Time.deltaTime * 2), 0, 0.5f);
            stepLength = stepDistance * 2;
            speed = Mathf.Clamp(Mathf.Lerp(speed, _velocity *1.5f, Time.deltaTime * 2), 2, 5);
            stepHeight = stepLength / 2;
        }
        

        Ray groundRay = new Ray(body.position + body.right * _footSpacing + Vector3.up * 2, Vector3.down);
        if(!otherFoot.IsMoving() && !IsMoving() && Physics.Raycast(groundRay, out RaycastHit hit, 10))
        {
            if(Vector3.Distance(_nextPos, hit.point) > stepDistance)
            {
                _stepProgressLerp = 0;
                //Determine if moving forwards or backwards
                int direction = body.InverseTransformPoint(hit.point).z > body.InverseTransformPoint(_nextPos).z ? 1 : -1;
                _nextPos = hit.point + body.forward * direction * stepLength + footPosOffset + (this.transform.forward * 0.12f);
                _nextNormal = hit.normal;
            }
        }
        if(_stepProgressLerp < 1)
        {
            Vector3 tempPos = Vector3.Lerp(_oldPos, _nextPos, _stepProgressLerp);
            tempPos.y += Mathf.Sin(_stepProgressLerp * Mathf.PI) * stepHeight;
            _currentPos= tempPos;
            _currentNormal = Vector3.Lerp(_oldNormal, _nextNormal, _stepProgressLerp);
            _stepProgressLerp += Time.deltaTime * speed;
        } else
        {
            _oldPos = _nextPos;
            _oldNormal = _nextNormal;
        }
    }

    public bool IsMoving()
    {
        return _stepProgressLerp < 1;
    }
}

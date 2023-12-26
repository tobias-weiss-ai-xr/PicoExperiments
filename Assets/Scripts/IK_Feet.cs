using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IK_Feet : MonoBehaviour
{
    public LayerMask groundLayer;
    public Transform body;
    public IK_Feet otherFoot;
    public float speed = 5, stepDistance = .3f, stepLength = .3f, stepHeight = .3f;
    public Vector3 footPosOffset, footRotOffset;

    private float _footSpacing, _stepProgressLerp;
    private Vector3 _oldPos, _currentPos, _nextPos;
    private Vector3 _oldNormal, _currentNormal, _nextNormal;
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
        transform.rotation = Quaternion.LookRotation(_currentNormal) *Quaternion.Euler(footRotOffset);

        Ray groundRay = new Ray(body.position + body.right * _footSpacing + Vector3.up * 2, Vector3.down);
        if(!otherFoot.IsMoving() && !IsMoving() && Physics.Raycast(groundRay, out RaycastHit hit, 10, groundLayer.value))
        {
            if(Vector3.Distance(_nextPos, hit.point) > stepDistance)
            {
                _stepProgressLerp = 0;
                //Determine if moving forwards or backwards
                int direction = body.InverseTransformPoint(hit.point).z > body.InverseTransformPoint(_nextPos).z ? 1 : -1;
                _nextPos = hit.point + body.forward * direction * stepLength + footPosOffset;
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

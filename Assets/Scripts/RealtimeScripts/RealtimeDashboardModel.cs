using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class RealtimeDashboardModel
{
    // https://normcore.io/documentation/realtime/synchronizing-custom-data.html#creating-a-realtime-model
    [RealtimeProperty(1, true, true)] // id, reliable?, change event
    private float _valueAriel;

    [RealtimeProperty(2, true, true)]
    private float _valueKuschelweich;

    [RealtimeProperty(3, true, true)]
    private float _valueOmo;

    [RealtimeProperty(4, true, true)] 
    private float _valueWeisserRiese;
}

/* ----- Begin Normal Autogenerated Code ----- */
public partial class RealtimeDashboardModel : RealtimeModel {
    public float valueAriel {
        get {
            return _valueArielProperty.value;
        }
        set {
            if (_valueArielProperty.value == value) return;
            _valueArielProperty.value = value;
            InvalidateReliableLength();
            FireValueArielDidChange(value);
        }
    }
    
    public float valueKuschelweich {
        get {
            return _valueKuschelweichProperty.value;
        }
        set {
            if (_valueKuschelweichProperty.value == value) return;
            _valueKuschelweichProperty.value = value;
            InvalidateReliableLength();
            FireValueKuschelweichDidChange(value);
        }
    }
    
    public float valueOmo {
        get {
            return _valueOmoProperty.value;
        }
        set {
            if (_valueOmoProperty.value == value) return;
            _valueOmoProperty.value = value;
            InvalidateReliableLength();
            FireValueOmoDidChange(value);
        }
    }
    
    public float valueWeisserRiese {
        get {
            return _valueWeisserRieseProperty.value;
        }
        set {
            if (_valueWeisserRieseProperty.value == value) return;
            _valueWeisserRieseProperty.value = value;
            InvalidateReliableLength();
            FireValueWeisserRieseDidChange(value);
        }
    }
    
    public delegate void PropertyChangedHandler<in T>(RealtimeDashboardModel model, T value);
    public event PropertyChangedHandler<float> valueArielDidChange;
    public event PropertyChangedHandler<float> valueKuschelweichDidChange;
    public event PropertyChangedHandler<float> valueOmoDidChange;
    public event PropertyChangedHandler<float> valueWeisserRieseDidChange;
    
    public enum PropertyID : uint {
        ValueAriel = 1,
        ValueKuschelweich = 2,
        ValueOmo = 3,
        ValueWeisserRiese = 4,
    }
    
    #region Properties
    
    private ReliableProperty<float> _valueArielProperty;
    
    private ReliableProperty<float> _valueKuschelweichProperty;
    
    private ReliableProperty<float> _valueOmoProperty;
    
    private ReliableProperty<float> _valueWeisserRieseProperty;
    
    #endregion
    
    public RealtimeDashboardModel() : base(null) {
        _valueArielProperty = new ReliableProperty<float>(1, _valueAriel);
        _valueKuschelweichProperty = new ReliableProperty<float>(2, _valueKuschelweich);
        _valueOmoProperty = new ReliableProperty<float>(3, _valueOmo);
        _valueWeisserRieseProperty = new ReliableProperty<float>(4, _valueWeisserRiese);
    }
    
    protected override void OnParentReplaced(RealtimeModel previousParent, RealtimeModel currentParent) {
        _valueArielProperty.UnsubscribeCallback();
        _valueKuschelweichProperty.UnsubscribeCallback();
        _valueOmoProperty.UnsubscribeCallback();
        _valueWeisserRieseProperty.UnsubscribeCallback();
    }
    
    private void FireValueArielDidChange(float value) {
        try {
            valueArielDidChange?.Invoke(this, value);
        } catch (System.Exception exception) {
            UnityEngine.Debug.LogException(exception);
        }
    }
    
    private void FireValueKuschelweichDidChange(float value) {
        try {
            valueKuschelweichDidChange?.Invoke(this, value);
        } catch (System.Exception exception) {
            UnityEngine.Debug.LogException(exception);
        }
    }
    
    private void FireValueOmoDidChange(float value) {
        try {
            valueOmoDidChange?.Invoke(this, value);
        } catch (System.Exception exception) {
            UnityEngine.Debug.LogException(exception);
        }
    }
    
    private void FireValueWeisserRieseDidChange(float value) {
        try {
            valueWeisserRieseDidChange?.Invoke(this, value);
        } catch (System.Exception exception) {
            UnityEngine.Debug.LogException(exception);
        }
    }
    
    protected override int WriteLength(StreamContext context) {
        var length = 0;
        length += _valueArielProperty.WriteLength(context);
        length += _valueKuschelweichProperty.WriteLength(context);
        length += _valueOmoProperty.WriteLength(context);
        length += _valueWeisserRieseProperty.WriteLength(context);
        return length;
    }
    
    protected override void Write(WriteStream stream, StreamContext context) {
        var writes = false;
        writes |= _valueArielProperty.Write(stream, context);
        writes |= _valueKuschelweichProperty.Write(stream, context);
        writes |= _valueOmoProperty.Write(stream, context);
        writes |= _valueWeisserRieseProperty.Write(stream, context);
        if (writes) InvalidateContextLength(context);
    }
    
    protected override void Read(ReadStream stream, StreamContext context) {
        var anyPropertiesChanged = false;
        while (stream.ReadNextPropertyID(out uint propertyID)) {
            var changed = false;
            switch (propertyID) {
                case (uint) PropertyID.ValueAriel: {
                    changed = _valueArielProperty.Read(stream, context);
                    if (changed) FireValueArielDidChange(valueAriel);
                    break;
                }
                case (uint) PropertyID.ValueKuschelweich: {
                    changed = _valueKuschelweichProperty.Read(stream, context);
                    if (changed) FireValueKuschelweichDidChange(valueKuschelweich);
                    break;
                }
                case (uint) PropertyID.ValueOmo: {
                    changed = _valueOmoProperty.Read(stream, context);
                    if (changed) FireValueOmoDidChange(valueOmo);
                    break;
                }
                case (uint) PropertyID.ValueWeisserRiese: {
                    changed = _valueWeisserRieseProperty.Read(stream, context);
                    if (changed) FireValueWeisserRieseDidChange(valueWeisserRiese);
                    break;
                }
                default: {
                    stream.SkipProperty();
                    break;
                }
            }
            anyPropertiesChanged |= changed;
        }
        if (anyPropertiesChanged) {
            UpdateBackingFields();
        }
    }
    
    private void UpdateBackingFields() {
        _valueAriel = valueAriel;
        _valueKuschelweich = valueKuschelweich;
        _valueOmo = valueOmo;
        _valueWeisserRiese = valueWeisserRiese;
    }
    
}
/* ----- End Normal Autogenerated Code ----- */
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Minis;   // jp.keijiro.minis

public class PrintMIDI : MonoBehaviour
{
    public PianoScript piano;
    void OnEnable()
    {
        // Register all existing devices
        foreach (var device in InputSystem.devices)
            Register(device);

        // Handle device add/remove
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;

        foreach (var device in InputSystem.devices)
            Unregister(device);
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added)
            Register(device);
        else if (change == InputDeviceChange.Removed)
            Unregister(device);
    }

    void Register(InputDevice device)
    {
        var midi = device as MidiDevice;
        if (midi == null) return;

        // Subscribe to MIDI callbacks
        midi.onWillNoteOn          += OnNoteOn;
        midi.onWillNoteOff         += OnNoteOff;
        midi.onWillAftertouch      += OnAftertouch;
        midi.onWillControlChange   += OnControlChange;
        midi.onWillChannelPressure += OnChannelPressure;
        midi.onWillPitchBend       += OnPitchBend;

        Debug.Log($"[MIDI] Registered: {midi.description}");
    }

    void Unregister(InputDevice device)
    {
        var midi = device as MidiDevice;
        if (midi == null) return;

        // Unsubscribe from MIDI callbacks
        midi.onWillNoteOn          -= OnNoteOn;
        midi.onWillNoteOff         -= OnNoteOff;
        midi.onWillAftertouch      -= OnAftertouch;
        midi.onWillControlChange   -= OnControlChange;
        midi.onWillChannelPressure -= OnChannelPressure;
        midi.onWillPitchBend       -= OnPitchBend;

        //Debug.Log($"[MIDI] Unregistered: {midi.description}");
    }

    // Note on: strike (initial hit)
    void OnNoteOn(MidiNoteControl note, float velocity)
    {
        piano.MIDINoteOn(note.noteNumber, velocity);  
        Debug.Log($"NOTE ON    dev={note.device} note={note.noteNumber} vel={velocity:0.000}");
    }

    // Note off: lift (release)
    void OnNoteOff(MidiNoteControl note)
    {
        piano.MIDINoteOff(note.noteNumber);
        Debug.Log($"NOTE OFF   dev={note.device} note={note.noteNumber}");
    }

    // Poly aftertouch: per-note pressure over time
    void OnAftertouch(MidiNoteControl note, float pressure)
    {
        Debug.Log($"AFTERTOUCH dev={note.device} note={note.noteNumber} pressure={pressure:0.000}");
    }

    // Control change: e.g., CC74 = slide (Y axis)
    void OnControlChange(MidiValueControl cc, float value)
    {
        if (piano != null) 
            piano.MIDIControlChange(value);
        Debug.Log($"CC         dev={cc.device} cc={cc.controlNumber} value={value:0.000}");
    }

    // Channel pressure: pressure per channel (not per note)
    void OnChannelPressure(AxisControl control, float pressure)
    {
        Debug.Log($"CH PRESS   dev={control.device.description} pressure={pressure:0.000}");
    }

    // Pitch bend: glide (X axis, pitch shift)
    void OnPitchBend(AxisControl control, float bend)
    {
        if (piano != null)
            piano.MIDIPitchBend(bend);
        
        Debug.Log($"PITCHBEND  dev={control.device.description} bend={bend:0.000}");
    }
}

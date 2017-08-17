/**
 * Copyright (c) 2017 Enzien Audio, Ltd.
 * 
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions, and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the phrase "powered by heavy",
 *    the heavy logo, and a hyperlink to https://enzienaudio.com, all in a visible
 *    form.
 * 
 *   2.1 If the Application is distributed in a store system (for example,
 *       the Apple "App Store" or "Google Play"), the phrase "powered by heavy"
 *       shall be included in the app description or the copyright text as well as
 *       the in the app itself. The heavy logo will shall be visible in the app
 *       itself as well.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using AOT;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Hv_slo_Granular_AudioLib))]
public class Hv_slo_Granular_Editor : Editor {

  [MenuItem("Heavy/slo_Granular")]
  static void CreateHv_slo_Granular() {
    GameObject target = Selection.activeGameObject;
    if (target != null) {
      target.AddComponent<Hv_slo_Granular_AudioLib>();
    }
  }
  
  private Hv_slo_Granular_AudioLib _dsp;

  private void OnEnable() {
    _dsp = target as Hv_slo_Granular_AudioLib;
  }

  public override void OnInspectorGUI() {
    bool isEnabled = _dsp.IsInstantiated();
    if (!isEnabled) {
      EditorGUILayout.LabelField("Press Play!",  EditorStyles.centeredGreyMiniLabel);
    }
    GUILayout.EndVertical();

    // parameters
    GUI.enabled = true;
    GUILayout.BeginVertical();
    EditorGUILayout.Space();
    EditorGUI.indentLevel++;
    
    // grainDel_vari
    GUILayout.BeginHorizontal();
    float grainDel_vari = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindel_vari);
    float newGraindel_vari = EditorGUILayout.Slider("grainDel_vari", grainDel_vari, 0.0f, 100.0f);
    if (grainDel_vari != newGraindel_vari) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindel_vari, newGraindel_vari);
    }
    GUILayout.EndHorizontal();
    
    // grainDelay
    GUILayout.BeginHorizontal();
    float grainDelay = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindelay);
    float newGraindelay = EditorGUILayout.Slider("grainDelay", grainDelay, 0.0f, 5000.0f);
    if (grainDelay != newGraindelay) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindelay, newGraindelay);
    }
    GUILayout.EndHorizontal();
    
    // grainDur_vari
    GUILayout.BeginHorizontal();
    float grainDur_vari = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindur_vari);
    float newGraindur_vari = EditorGUILayout.Slider("grainDur_vari", grainDur_vari, 0.0f, 100.0f);
    if (grainDur_vari != newGraindur_vari) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindur_vari, newGraindur_vari);
    }
    GUILayout.EndHorizontal();
    
    // grainDuration
    GUILayout.BeginHorizontal();
    float grainDuration = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainduration);
    float newGrainduration = EditorGUILayout.Slider("grainDuration", grainDuration, 0.0f, 200.0f);
    if (grainDuration != newGrainduration) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainduration, newGrainduration);
    }
    GUILayout.EndHorizontal();
    
    // grainPos_vari
    GUILayout.BeginHorizontal();
    float grainPos_vari = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainpos_vari);
    float newGrainpos_vari = EditorGUILayout.Slider("grainPos_vari", grainPos_vari, 0.0f, 100.0f);
    if (grainPos_vari != newGrainpos_vari) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainpos_vari, newGrainpos_vari);
    }
    GUILayout.EndHorizontal();
    
    // grainPosition
    GUILayout.BeginHorizontal();
    float grainPosition = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainposition);
    float newGrainposition = EditorGUILayout.Slider("grainPosition", grainPosition, 0.0f, 1.0f);
    if (grainPosition != newGrainposition) {
			Debug.Log ("hello I'm here");

      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainposition, newGrainposition);
    }
    GUILayout.EndHorizontal();
    
    // grainRate
    GUILayout.BeginHorizontal();
    float grainRate = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate);
    float newGrainrate = EditorGUILayout.Slider("grainRate", grainRate, 0.0f, 2.0f);
    if (grainRate != newGrainrate) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate, newGrainrate);
    }
    GUILayout.EndHorizontal();
    
    // grainRate_vari
    GUILayout.BeginHorizontal();
    float grainRate_vari = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate_vari);
    float newGrainrate_vari = EditorGUILayout.Slider("grainRate_vari", grainRate_vari, 0.0f, 100.0f);
    if (grainRate_vari != newGrainrate_vari) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate_vari, newGrainrate_vari);
    }
    GUILayout.EndHorizontal();
    
    // grainVolume
    GUILayout.BeginHorizontal();
    float grainVolume = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainvolume);
    float newGrainvolume = EditorGUILayout.Slider("grainVolume", grainVolume, 0.0f, 1.0f);
    if (grainVolume != newGrainvolume) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainvolume, newGrainvolume);
    }
    GUILayout.EndHorizontal();
    
    // metro
    GUILayout.BeginHorizontal();
    float metro = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Metro);
    float newMetro = EditorGUILayout.Slider("metro", metro, 0.0f, 1.0f);
    if (metro != newMetro) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Metro, newMetro);
    }
    GUILayout.EndHorizontal();
    
    // source_Length
    GUILayout.BeginHorizontal();
    float source_Length = _dsp.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Source_length);
    float newSource_length = EditorGUILayout.Slider("source_Length", source_Length, 0.0f, 1323000.0f);
    if (source_Length != newSource_length) {
      _dsp.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Source_length, newSource_length);
    }
    GUILayout.EndHorizontal();
    EditorGUI.indentLevel--;
  }
}
#endif // UNITY_EDITOR

[RequireComponent (typeof (AudioSource))]
public class Hv_slo_Granular_AudioLib : MonoBehaviour {
  
  // Parameters are used to send float messages into the patch context (thread-safe).
  // Example usage:
  /*
    void Start () {
        Hv_slo_Granular_AudioLib script = GetComponent<Hv_slo_Granular_AudioLib>();
        // Get and set a parameter
        float grainDel_vari = script.GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindel_vari);
        script.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindel_vari, grainDel_vari + 0.1f);
    }
  */
  public enum Parameter : uint {
    Graindel_vari = 0x84FE9908,
    Graindelay = 0x5B541F0D,
    Graindur_vari = 0x27CD066F,
    Grainduration = 0xA85E3A53,
    Grainpos_vari = 0x88BC54B0,
    Grainposition = 0x3283ACD4,
    Grainrate = 0x5B7EAA71,
    Grainrate_vari = 0x9EAAFCD9,
    Grainvolume = 0xC20246C1,
    Metro = 0x9E78BC0,
    Source_length = 0xB8856A5E,
  }
  
  // Delegate method for receiving float messages from the patch context (thread-safe).
  // Example usage:
  /*
    void Start () {
        Hv_slo_Granular_AudioLib script = GetComponent<Hv_slo_Granular_AudioLib>();
        script.RegisterSendHook();
        script.FloatReceivedCallback += OnFloatMessage;
    }

    void OnFloatMessage(Hv_slo_Granular_AudioLib.FloatMessage message) {
        Debug.Log(message.receiverName + ": " + message.value);
    }
  */
  public class FloatMessage {
    public string receiverName;
    public float value;

    public FloatMessage(string name, float x) {
      receiverName = name;
      value = x;
    }
  }
  public delegate void FloatMessageReceived(FloatMessage message);
  public FloatMessageReceived FloatReceivedCallback;
  public float grainDel_vari = 5.0f;
  public float grainDelay = 0.0f;
  public float grainDur_vari = 5.0f;
  public float grainDuration = 100.0f;
  public float grainPos_vari = 1.0f;
  public float grainPosition = 0.5f;
  public float grainRate = 1.0f;
  public float grainRate_vari = 1.0f;
  public float grainVolume = 1.0f;
  public float metro = 0.0f;
  public float source_Length = 441000.0f;

  // internal state
  private Hv_slo_Granular_Context _context;

  public bool IsInstantiated() {
    return (_context != null);
  }

  public void RegisterSendHook() {
    _context.RegisterSendHook();
  }
  
  // see Hv_slo_Granular_AudioLib.Parameter for definitions
  public float GetFloatParameter(Hv_slo_Granular_AudioLib.Parameter param) {
    switch (param) {
      case Parameter.Graindel_vari: return grainDel_vari;
      case Parameter.Graindelay: return grainDelay;
      case Parameter.Graindur_vari: return grainDur_vari;
      case Parameter.Grainduration: return grainDuration;
      case Parameter.Grainpos_vari: return grainPos_vari;
      case Parameter.Grainposition: return grainPosition;
      case Parameter.Grainrate: return grainRate;
      case Parameter.Grainrate_vari: return grainRate_vari;
      case Parameter.Grainvolume: return grainVolume;
      case Parameter.Metro: return metro;
      case Parameter.Source_length: return source_Length;
      default: return 0.0f;
    }
  }

  public void SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter param, float x) {
    switch (param) {
      case Parameter.Graindel_vari: {
        x = Mathf.Clamp(x, 0.0f, 100.0f);
        grainDel_vari = x;
        break;
      }
      case Parameter.Graindelay: {
        x = Mathf.Clamp(x, 0.0f, 5000.0f);
        grainDelay = x;
        break;
      }
      case Parameter.Graindur_vari: {
        x = Mathf.Clamp(x, 0.0f, 100.0f);
        grainDur_vari = x;
        break;
      }
      case Parameter.Grainduration: {
        x = Mathf.Clamp(x, 0.0f, 200.0f);
        grainDuration = x;
        break;
      }
      case Parameter.Grainpos_vari: {
        x = Mathf.Clamp(x, 0.0f, 100.0f);
        grainPos_vari = x;
        break;
      }
      case Parameter.Grainposition: {
        x = Mathf.Clamp(x, 0.0f, 1.0f);
        grainPosition = x;
        break;
      }
      case Parameter.Grainrate: {
        x = Mathf.Clamp(x, 0.0f, 2.0f);
        grainRate = x;
        break;
      }
      case Parameter.Grainrate_vari: {
        x = Mathf.Clamp(x, 0.0f, 100.0f);
        grainRate_vari = x;
        break;
      }
      case Parameter.Grainvolume: {
        x = Mathf.Clamp(x, 0.0f, 1.0f);
        grainVolume = x;
        break;
      }
      case Parameter.Metro: {
        x = Mathf.Clamp(x, 0.0f, 1.0f);
        metro = x;
        break;
      }
      case Parameter.Source_length: {
        x = Mathf.Clamp(x, 0.0f, 1323000.0f);
        source_Length = x;
        break;
      }
      default: return;
    }
    if (IsInstantiated()) _context.SendFloatToReceiver((uint) param, x);
  }
  
  public void FillTableWithMonoAudioClip(string tableName, AudioClip clip) {
    if (clip.channels > 1) {
      Debug.LogWarning("Hv_slo_Granular_AudioLib: Only loading first channel of '" +
          clip.name + "' into table '" +
          tableName + "'. Multi-channel files are not supported.");
    }
    float[] buffer = new float[clip.samples]; // copy only the 1st channel
    clip.GetData(buffer, 0);
    _context.FillTableWithFloatBuffer(tableName, buffer);
  }

  public void FillTableWithFloatBuffer(string tableName, float[] buffer) {
    _context.FillTableWithFloatBuffer(tableName, buffer);
  }

  private void Awake() {
    _context = new Hv_slo_Granular_Context((double) AudioSettings.outputSampleRate);
  }
  
  private void Start() {
    _context.SendFloatToReceiver((uint) Parameter.Graindel_vari, grainDel_vari);
    _context.SendFloatToReceiver((uint) Parameter.Graindelay, grainDelay);
    _context.SendFloatToReceiver((uint) Parameter.Graindur_vari, grainDur_vari);
    _context.SendFloatToReceiver((uint) Parameter.Grainduration, grainDuration);
    _context.SendFloatToReceiver((uint) Parameter.Grainpos_vari, grainPos_vari);
    _context.SendFloatToReceiver((uint) Parameter.Grainposition, grainPosition);
    _context.SendFloatToReceiver((uint) Parameter.Grainrate, grainRate);
    _context.SendFloatToReceiver((uint) Parameter.Grainrate_vari, grainRate_vari);
    _context.SendFloatToReceiver((uint) Parameter.Grainvolume, grainVolume);
    _context.SendFloatToReceiver((uint) Parameter.Metro, metro);
    _context.SendFloatToReceiver((uint) Parameter.Source_length, source_Length);
  }
  
  private void Update() {
    // retreive sent messages
    if (_context.IsSendHookRegistered()) {
      Hv_slo_Granular_AudioLib.FloatMessage tempMessage;
      while ((tempMessage = _context.msgQueue.GetNextMessage()) != null) {
        FloatReceivedCallback(tempMessage);
      }
    }
  }

  private void OnAudioFilterRead(float[] buffer, int numChannels) {
    Assert.AreEqual(numChannels, _context.GetNumOutputChannels()); // invalid channel configuration
    _context.Process(buffer, buffer.Length / numChannels); // process dsp
  }
}

class Hv_slo_Granular_Context {

#if UNITY_IOS && !UNITY_EDITOR
  private const string _dllName = "__Internal";
#else
  private const string _dllName = "Hv_slo_Granular_AudioLib";
#endif

  // Thread-safe message queue
  public class SendMessageQueue {
    private readonly object _msgQueueSync = new object();
    private readonly Queue<Hv_slo_Granular_AudioLib.FloatMessage> _msgQueue = new Queue<Hv_slo_Granular_AudioLib.FloatMessage>();

    public Hv_slo_Granular_AudioLib.FloatMessage GetNextMessage() {
      lock (_msgQueueSync) {
        return (_msgQueue.Count != 0) ? _msgQueue.Dequeue() : null;
      }
    }

    public void AddMessage(string receiverName, float value) {
      Hv_slo_Granular_AudioLib.FloatMessage msg = new Hv_slo_Granular_AudioLib.FloatMessage(receiverName, value);
      lock (_msgQueueSync) {
        _msgQueue.Enqueue(msg);
      }
    }
  }

  public readonly SendMessageQueue msgQueue = new SendMessageQueue();
  private readonly GCHandle gch;
  private readonly IntPtr _context; // handle into unmanaged memory
  private SendHook _sendHook = null;

  [DllImport (_dllName)]
  private static extern IntPtr hv_slo_Granular_new_with_options(double sampleRate, int poolKb, int inQueueKb, int outQueueKb);

  [DllImport (_dllName)]
  private static extern int hv_processInlineInterleaved(IntPtr ctx,
      [In] float[] inBuffer, [Out] float[] outBuffer, int numSamples);

  [DllImport (_dllName)]
  private static extern void hv_delete(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern double hv_getSampleRate(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern int hv_getNumInputChannels(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern int hv_getNumOutputChannels(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern void hv_setSendHook(IntPtr ctx, SendHook sendHook);

  [DllImport (_dllName)]
  private static extern void hv_setPrintHook(IntPtr ctx, PrintHook printHook);

  [DllImport (_dllName)]
  private static extern int hv_setUserData(IntPtr ctx, IntPtr userData);

  [DllImport (_dllName)]
  private static extern IntPtr hv_getUserData(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern void hv_sendBangToReceiver(IntPtr ctx, uint receiverHash);

  [DllImport (_dllName)]
  private static extern void hv_sendFloatToReceiver(IntPtr ctx, uint receiverHash, float x);

  [DllImport (_dllName)]
  private static extern uint hv_msg_getTimestamp(IntPtr message);

  [DllImport (_dllName)]
  private static extern bool hv_msg_hasFormat(IntPtr message, string format);

  [DllImport (_dllName)]
  private static extern float hv_msg_getFloat(IntPtr message, int index);

  [DllImport (_dllName)]
  private static extern bool hv_table_setLength(IntPtr ctx, uint tableHash, uint newSampleLength);

  [DllImport (_dllName)]
  private static extern IntPtr hv_table_getBuffer(IntPtr ctx, uint tableHash);

  [DllImport (_dllName)]
  private static extern float hv_samplesToMilliseconds(IntPtr ctx, uint numSamples);

  [DllImport (_dllName)]
  private static extern uint hv_stringToHash(string s);

  private delegate void PrintHook(IntPtr context, string printName, string str, IntPtr message);

  private delegate void SendHook(IntPtr context, string sendName, uint sendHash, IntPtr message);

  public Hv_slo_Granular_Context(double sampleRate, int poolKb=10, int inQueueKb=2, int outQueueKb=2) {
    gch = GCHandle.Alloc(msgQueue);
    _context = hv_slo_Granular_new_with_options(sampleRate, poolKb, inQueueKb, outQueueKb);
    hv_setPrintHook(_context, new PrintHook(OnPrint));
    hv_setUserData(_context, GCHandle.ToIntPtr(gch));
  }

  ~Hv_slo_Granular_Context() {
    hv_delete(_context);
    GC.KeepAlive(_context);
    GC.KeepAlive(_sendHook);
    gch.Free();
  }

  public void RegisterSendHook() {
    // Note: send hook functionality only applies to messages containing a single float value
    if (_sendHook == null) {
      _sendHook = new SendHook(OnMessageSent);
      hv_setSendHook(_context, _sendHook);
    }
  }

  public bool IsSendHookRegistered() {
    return (_sendHook != null);
  }

  public double GetSampleRate() {
    return hv_getSampleRate(_context);
  }

  public int GetNumInputChannels() {
    return hv_getNumInputChannels(_context);
  }

  public int GetNumOutputChannels() {
    return hv_getNumOutputChannels(_context);
  }

  public void SendBangToReceiver(uint receiverHash) {
    hv_sendBangToReceiver(_context, receiverHash);
  }

  public void SendFloatToReceiver(uint receiverHash, float x) {
    hv_sendFloatToReceiver(_context, receiverHash, x);
  }

  public void FillTableWithFloatBuffer(string tableName, float[] buffer) {
    uint tableHash = hv_stringToHash(tableName);
    if (hv_table_getBuffer(_context, tableHash) != IntPtr.Zero) {
      hv_table_setLength(_context, tableHash, (uint) buffer.Length);
      Marshal.Copy(buffer, 0, hv_table_getBuffer(_context, tableHash), buffer.Length);
    } else {
      Debug.Log(string.Format("Table '{0}' doesn't exist in the patch context.", tableName));
    }
  }

  public uint StringToHash(string s) {
    return hv_stringToHash(s);
  }

  public int Process(float[] buffer, int numFrames) {
    return hv_processInlineInterleaved(_context, buffer, buffer, numFrames);
  }

  [MonoPInvokeCallback(typeof(PrintHook))]
  private static void OnPrint(IntPtr context, string printName, string str, IntPtr message) {
    float timeInSecs = hv_samplesToMilliseconds(context, hv_msg_getTimestamp(message)) / 1000.0f;
//    Debug.Log(string.Format("{0} [{1:0.000}]: {2}", printName, timeInSecs, str));
  }

  [MonoPInvokeCallback(typeof(SendHook))]
  private static void OnMessageSent(IntPtr context, string sendName, uint sendHash, IntPtr message) {
    if (hv_msg_hasFormat(message, "f")) {
      SendMessageQueue msgQueue = (SendMessageQueue) GCHandle.FromIntPtr(hv_getUserData(context)).Target;
      msgQueue.AddMessage(sendName, hv_msg_getFloat(message, 0));
    }
  }
}

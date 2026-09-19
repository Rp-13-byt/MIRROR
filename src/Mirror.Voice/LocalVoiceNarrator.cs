using System.Runtime.Versioning;
using System.Speech.Synthesis;

namespace Mirror.Voice;

[SupportedOSPlatform("windows")]
public class LocalVoiceNarrator : IDisposable
{
 private SpeechSynthesizer? _synthesizer;
 private bool _isPaused;
 private bool _isSpeaking;

 public event Action<bool>? PlaybackStateChanged;

 public bool IsSpeaking => _isSpeaking;
 public bool IsPaused => _isPaused;

 public LocalVoiceNarrator()
 {
 try
 {
 _synthesizer = new SpeechSynthesizer();
 _synthesizer.Rate = 0; // Default human pace
 _synthesizer.Volume = 100;
 _synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;
 }
 catch
 {
 _synthesizer = null;
 }
 }

 public void Speak(string text)
 {
 if (_synthesizer == null || string.IsNullOrWhiteSpace(text)) return;

 Stop();

 _isSpeaking = true;
 _isPaused = false;
 PlaybackStateChanged?.Invoke(true);

 try
 {
 _synthesizer.SpeakAsync(text);
 }
 catch
 {
 _isSpeaking = false;
 PlaybackStateChanged?.Invoke(false);
 }
 }

 public void Pause()
 {
 if (_synthesizer != null && _isSpeaking && !_isPaused)
 {
 try
 {
 _synthesizer.Pause();
 _isPaused = true;
 PlaybackStateChanged?.Invoke(false);
 }
 catch { }
 }
 }

 public void Resume()
 {
 if (_synthesizer != null && _isSpeaking && _isPaused)
 {
 try
 {
 _synthesizer.Resume();
 _isPaused = false;
 PlaybackStateChanged?.Invoke(true);
 }
 catch { }
 }
 }

 public void Stop()
 {
 if (_synthesizer != null)
 {
 try
 {
 _synthesizer.SpeakAsyncCancelAll();
 }
 catch { }
 }

 _isSpeaking = false;
 _isPaused = false;
 PlaybackStateChanged?.Invoke(false);
 }

 private void Synthesizer_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
 {
 _isSpeaking = false;
 _isPaused = false;
 PlaybackStateChanged?.Invoke(false);
 }

 public void Dispose()
 {
 Stop();
 if (_synthesizer != null)
 {
 _synthesizer.SpeakCompleted -= Synthesizer_SpeakCompleted;
 _synthesizer.Dispose();
 _synthesizer = null;
 }
 }
}
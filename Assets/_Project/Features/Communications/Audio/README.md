# Communications Audio

Radio DSP, procedural voice, PTT SFX, and swappable TTS/STT adapters (Phase 8).

- **F8** — incoming audio smoke (beep + static + procedural voice)
- **Hold V** (L3 on gamepad) — push-to-talk outgoing stub line
- Optional `RadioAudioProfile` asset via **Dark Matter: Genesis → Communications → Radio Audio Profile**

Phase 8.1: swap `ProceduralRadioVoiceSynthesizer` / `StubRadioSpeechRecognizer` for LocalVoiceLLM adapters when SimpleOffline STT/TTS modules are installed.

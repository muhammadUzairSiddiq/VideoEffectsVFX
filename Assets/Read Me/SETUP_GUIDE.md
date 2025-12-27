# Quick Setup Guide - Preset System & Effect Panel

## ✅ What's New

### 1. **JSON Preset System**
- Creates `EffectPreset.json` on first video upload
- Location: `Application.persistentDataPath/EffectPreset.json`
- Stores default effect parameters (zoom, shake, RGB, bloom, vignette, etc.)
- Can be modified to create different presets

### 2. **Effect Panel Controller**
- **Open/Close Effects Panel**: Toggle buttons to show/hide PRESETS panel
- **Individual Effect Buttons**: Click any effect (Zoom, Bloom, etc.) to activate ONLY that effect
- **Randomize Button**: Click to enable ALL effects (current random behavior)

### 3. **Single Effect Mode**
- When user clicks individual effect button → Only that effect triggers on beats
- When user clicks Randomize → All effects work randomly (original behavior)

---

## 🚀 Setup Steps

### Step 1: Add Components to Scene

1. **Add `EffectPresetManager` component:**
   - Create empty GameObject named "EffectPresetManager"
   - Add `EffectPresetManager.cs` component
   - (Auto-created if missing, but better to add manually)

2. **Add `EffectPanelController` component:**
   - Add to any GameObject in scene (or create new one)
   - Or add to existing controller GameObject
   - Component will auto-find buttons and panel

### Step 2: Assign UI References (Optional - Auto-finds if not assigned)

**EffectPanelController:**
- `presetsPanel` → Drag "PRESETS" GameObject from Hierarchy
- `openEffectsButton` → Drag "EFFECT OPEN Button"
- `closeEffectsButton` → Drag "EFFECT CLOSE Button"
- Individual effect buttons → Auto-found by name

### Step 3: Verify Button Names

Make sure your buttons are named correctly (case-insensitive):
- "EFFECT OPEN Button" or "Open Effect" → Opens panel
- "EFFECT CLOSE Button" or "Close Effect" → Closes panel
- "Zoom Effect" → Triggers zoom only
- "Bloom Effect" → Triggers bloom only
- "Glitch Effect" or "Glitchy Effect" → Triggers glitch only
- "Vignette Effect" → Triggers vignette only
- "Shake Effect" or "Shaky Effect" → Triggers shake only
- "RGB Effect" or "RGB Wavy Effect" → Triggers MaterialRGB only
- "Rotate Effect" → Triggers rotate only
- "Lens Distort Effect" or "Lens Distortion Effect" → Triggers lens distortion only

### Step 4: Test

1. **Upload a video** → JSON preset created automatically
2. **Click "Open Effects"** → Panel opens, close button enabled
3. **Click any effect button** → Only that effect triggers on beats
4. **Click "Randomize"** → All effects work randomly again
5. **Click "Close Effects"** → Panel closes, open button enabled

---

## 📁 File Locations

- **Preset JSON**: `Application.persistentDataPath/EffectPreset.json`
  - Editor: `C:/Users/[User]/AppData/LocalLow/[Company]/[App]/EffectPreset.json`
  - Android: `/storage/emulated/0/Android/data/[Package]/files/EffectPreset.json`

---

## 🔧 How It Works

### Preset System Flow:
1. User uploads video → `VideoUploadController.OnPrepared()` called
2. `EffectPresetManager.LoadOrCreatePreset()` → Creates JSON if missing
3. `EffectPresetManager.ApplyPresetToEffects()` → Applies values to all effects
4. Effects use preset values as base

### Effect Panel Flow:
1. **Panel starts closed** → Open button enabled, Close button disabled
2. **User clicks Open** → Panel opens, Open disabled, Close enabled
3. **User clicks effect button** → `BeatEffectController.SetSingleEffectMode()` → Only that effect triggers
4. **User clicks Randomize** → `BeatEffectController.SetAllEffectsMode()` → All effects random

---

## 🎯 Key Features

✅ **Auto-find**: All components auto-find references if not assigned  
✅ **JSON Preset**: Created on first video upload  
✅ **Panel Toggle**: Open/Close buttons work automatically  
✅ **Single Effect Mode**: Click effect button = only that effect  
✅ **Randomize Mode**: Click randomize = all effects random  
✅ **No Manual Setup**: Works out of the box with default names  

---

## ⚠️ Notes

- Preset JSON is created on **first video upload** only
- Modify JSON file directly to change preset values
- Individual effect buttons disable random mode automatically
- Randomize button re-enables random mode
- Panel state persists during video playback


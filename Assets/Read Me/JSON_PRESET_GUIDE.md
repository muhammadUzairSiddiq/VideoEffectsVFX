# JSON Preset File Guide

## 📁 File Location

### **Editor (Windows):**
```
C:\Users\[YourUsername]\AppData\LocalLow\[CompanyName]\[AppName]\EffectPreset.json
```
Example:
```
C:\Users\hp\AppData\LocalLow\DefaultCompany\VideoFXPrototype\EffectPreset.json
```

### **Android:**
```
/storage/emulated/0/Android/data/[PackageName]/files/EffectPreset.json
```
Example:
```
/storage/emulated/0/Android/data/com.yourcompany.videofx/files/EffectPreset.json
```

### **How to Find on Android:**
1. Use a file manager app (like ES File Explorer, File Manager)
2. Navigate to: `Android/data/[YourPackageName]/files/`
3. Look for `EffectPreset.json`

---

## 🔍 How to Check if JSON File Exists

### **Method 1: Debug Logs (Editor)**
When you upload a video, check the Unity Console for these logs:

**If file EXISTS:**
```
✅ EffectPresetManager: JSON file EXISTS and loaded successfully!
📁 File path: [path]
📄 JSON content preview: [preview]
🎨 Loaded values - Zoom: 1.2, Bloom: 1.5, RGB: 0.3
```

**If file DOESN'T EXIST:**
```
⚠️ EffectPresetManager: JSON file NOT FOUND at [path]
📝 Creating new default preset file...
✅ EffectPresetManager: Created NEW default preset file!
📁 File saved to: [path]
```

### **Method 2: Check File Path in Logs**
Look for this log at app start:
```
EffectPresetManager: Preset file path = [full path]
EffectPresetManager: Persistent data path = [data path]
```

### **Method 3: Manual Check**
1. **Editor**: Navigate to the path shown in logs
2. **Android**: Use file manager app (see paths above)

---

## ✅ Testing if JSON File Works

### **Step 1: Upload a Video**
- Upload any video → JSON file is created automatically
- Check console logs to see file path

### **Step 2: Edit JSON File**
Open `EffectPreset.json` and change some values:

**Before:**
```json
{
    "zoomStrength": 1.2,
    "bloomIntensity": 1.5,
    "rgbIntensity": 0.3
}
```

**After (example):**
```json
{
    "zoomStrength": 2.0,
    "bloomIntensity": 3.0,
    "rgbIntensity": 0.8
}
```

### **Step 3: Test Changes**
**Option A: Restart App**
- Close app completely
- Reopen app
- Upload video again
- New values will be applied

**Option B: Use Reload Method (if added to UI)**
- Call `EffectPresetManager.Instance.ReloadPreset()`
- Upload video again

### **Step 4: Verify**
- Check console logs - should show new values:
```
🎨 Loaded values - Zoom: 2.0, Bloom: 3.0, RGB: 0.8
✅ Applied Zoom: strength=2.0, speed=3.0
✅ Applied Bloom: intensity=3.0, scatter=0.7
```

---

## 📋 Default JSON Structure

```json
{
    "zoomStrength": 1.2,
    "zoomSpeed": 3.0,
    "shakeIntensity": 0.1,
    "shakeSpeed": 30.0,
    "rgbIntensity": 0.3,
    "rgbSpeed": 5.0,
    "bloomIntensity": 1.5,
    "bloomScatter": 0.7,
    "vignetteIntensity": 0.4,
    "vignetteSmoothness": 0.5,
    "lensDistortionIntensity": 0.3,
    "rotationIntensity": 2.0,
    "rotationSpeed": 8.0,
    "colorIntensity": 0.15,
    "fadeSpeed": 5.0,
    "glitchIntensity": 0.5
}
```

---

## 🐛 Troubleshooting

### **File Not Created?**
- Check console for errors
- Verify `Application.persistentDataPath` is accessible
- Check file permissions (especially on Android)

### **Changes Not Applied?**
- Make sure JSON syntax is valid (no typos, proper brackets)
- Restart app after editing JSON
- Check console logs to see if values were loaded

### **Can't Find File on Mobile?**
- Use ADB: `adb shell run-as [package] ls files/`
- Or use a file manager app with root access
- Check logs for exact path

---

## 💡 Tips

1. **Backup JSON**: Save a copy before editing
2. **Test Incrementally**: Change one value at a time
3. **Check Logs**: Always check console for confirmation
4. **Valid JSON**: Use a JSON validator if unsure about syntax

---

## 📱 Mobile Debugging

Since mobile has no debug console, use these methods:

1. **Log to File**: Add file logging (write logs to a text file)
2. **UI Display**: Show file path in app UI (temporary debug text)
3. **ADB Logcat**: `adb logcat | grep EffectPresetManager`
4. **File Manager**: Manually check if file exists at the path

---

## 🔄 How It Works

1. **First Upload** → Creates `EffectPreset.json` with default values
2. **Subsequent Uploads** → Reads existing `EffectPreset.json`
3. **Values Applied** → All effects use values from JSON
4. **Edit JSON** → Restart app → New values applied

---

## ✅ Verification Checklist

- [ ] JSON file created on first video upload
- [ ] File path shown in console logs
- [ ] Can find file at specified path
- [ ] Can edit JSON file
- [ ] Changes apply after restart
- [ ] Console shows loaded values
- [ ] Effects use JSON values (not defaults)


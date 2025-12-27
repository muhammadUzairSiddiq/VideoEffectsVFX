# NatCorder Setup Guide

## ✅ What's Implemented

1. **Effect Timeline Tracking** - Records when user clicks effect buttons
2. **NatCorder Integration** - Uses MP4Recorder for smooth video export
3. **Export Button Control** - Disabled during playback, enabled when video ends
4. **Effect Sequence Replay** - Exports video with exact same effect sequence

---

## 🚀 Quick Setup

### Step 1: Add EffectTimelineTracker Component
- Create empty GameObject (or use existing)
- Add `EffectTimelineTracker.cs` component
- (Auto-found if not assigned)

### Step 2: Verify Export Button
- Export button should already exist in your scene
- Component will auto-find it
- Button will be disabled by default
- Enabled automatically when video ends

### Step 3: Test
1. Upload video
2. Click effect buttons during playback (Zoom, Rotate, Shake, etc.)
3. Wait for video to end
4. Export button becomes enabled
5. Click Export → Video exports with all effects in sequence

---

## 📋 How It Works

### During Playback:
- User clicks "Zoom" at 5s → Recorded: `{time: 5s, effect: "Zoom"}`
- User clicks "Rotate" at 15s → Recorded: `{time: 15s, effect: "Rotate"}`
- User clicks "Shake" at 25s → Recorded: `{time: 25s, effect: "Shake"}`

### During Export:
- Video replays from start
- At 5s → Switches to Zoom effect
- At 15s → Switches to Rotate effect
- At 25s → Switches to Shake effect
- All effects recorded perfectly!

---

## ⚙️ Settings

**Export Settings (in VideoExporter):**
- Resolution: 1080x1920 (vertical video)
- Frame Rate: 30fps
- Audio Bitrate: 96kbps

**To adjust for low-end devices:**
- Reduce resolution: 720x1280
- Reduce frame rate: 24fps

---

## 📁 Export Location

**Editor:**
```
C:\Users\[User]\AppData\LocalLow\[Company]\[App]\Exports\VideoFX_[timestamp].mp4
```

**Android:**
```
/storage/emulated/0/Android/data/[Package]/files/Exports/VideoFX_[timestamp].mp4
```

---

## ✅ Features

- ✅ Zero lag recording (background replay)
- ✅ Exact effect sequence preserved
- ✅ All visual effects recorded
- ✅ Audio included
- ✅ Export button auto-enable/disable
- ✅ Loading panel during export
- ✅ Works on Android/iOS

---

## 🐛 Troubleshooting

**Export button not enabling?**
- Check VideoUploadController calls `videoExporter.OnVideoEnded()`
- Check export button is assigned

**Effects not replaying?**
- Check EffectTimelineTracker component exists
- Check timeline has recorded effects (check console logs)

**Recording lag?**
- Reduce resolution to 720x1280
- Reduce frame rate to 24fps

---

## 📝 Notes

- Timeline is cleared when new video starts
- If no timeline exists, uses current effect mode (randomize or last selected)
- Export happens in background (replay + record)
- User sees smooth playback, recording happens hidden


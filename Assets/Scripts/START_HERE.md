# 🎮 Shop System - START HERE

## What You Got

A complete **CloudCoin Shop System** where players can buy agent skins with virtual currency!

## 📖 Documentation Overview

```
┌─────────────────────────────────────────────────────┐
│          SHOP SYSTEM DOCUMENTATION MAP              │
└─────────────────────────────────────────────────────┘

START_HERE.md  ◄── YOU ARE HERE (Quick overview)
    │
    ├─► SHOP_SYSTEM_COMPLETE.md
    │   └─► 📘 Main guide with Quick Start (5 min setup)
    │
    ├─► ShopSystemIntegrationChecklist.md
    │   └─► ✅ Complete checklist of what's done/todo
    │
    ├─► ShopSystemArchitecture.md
    │   └─► 🏗️ Technical architecture & diagrams
    │
    ├─► QuickShopSetup.md
    │   └─► ⚡ Minimal setup steps
    │
    └─► ShopUISetupGuide.md
        └─► 🎨 Detailed UI creation guide
```

## 🚀 Get Started in 3 Steps

### 1️⃣ Configure Prices (1 minute)
```
Menu → TPSBR/Shop System Setup Helper
   └─► Set prices
       └─► Click "Apply Agent Prices"
```

### 2️⃣ Create UI (3 minutes)
```
Menu → TPSBR/Create Shop UI
   ├─► Click "Create UIShopItem Widget"
   │   └─► Save as prefab
   │
   └─► Click "Create UIShopView Panel"
       └─► Save as prefab
```

### 3️⃣ Add Shop Button (1 minute)
```
In Menu scene:
   ├─► Duplicate any menu button
   ├─► Rename to "ShopButton"
   ├─► Change text to "SHOP"
   └─► Link to UIMainMenuView component
```

## ✅ What's Already Done

✅ CloudCoin currency system  
✅ Shop purchase logic  
✅ Agent ownership tracking  
✅ PlayerData integration  
✅ UI scripts (UIShopView, UIShopItem)  
✅ Agent selection filtering  
✅ Main menu button support  
✅ Debug/testing tools  
✅ **Two automated editor tools**

## 🔲 What You Need to Do

🔲 Create UI prefabs (use editor tool!)  
🔲 Add shop button to main menu  
🔲 Configure agent prices  
🔲 Test!

## 🛠️ Editor Tools

### `TPSBR/Create Shop UI`
Creates UI GameObjects automatically
- Creates UIShopItem widget structure
- Creates UIShopView panel structure

### `TPSBR/Shop System Setup Helper`
Configures prices automatically
- Set Soldier cost (default: 0)
- Set Marine cost (default: 500)
- Applies to AgentSettings

## 🧪 Testing

Add `ShopSystemDebugHelper` component to any GameObject:
- Right-click → "Add 1000 CloudCoins"
- Right-click → "Log CloudCoins"
- Right-click → "Log Owned Agents"

## 📚 Read Next

**For setup:** Read `SHOP_SYSTEM_COMPLETE.md`  
**For checklist:** Read `ShopSystemIntegrationChecklist.md`  
**For architecture:** Read `ShopSystemArchitecture.md`

## 💡 Key Features

- 💰 CloudCoin virtual currency
- 🛒 Agent shop with purchases
- 🔒 Locked agents (Marine requires purchase)
- ✅ Default agent (Soldier is free)
- 💾 Persistent data
- 🎨 Fully customizable UI
- 🧪 Debug tools included

## 🎯 Default Configuration

| Agent   | Cost | Status        |
|---------|------|---------------|
| Soldier | 0    | ✅ Free       |
| Marine  | 500  | 🔒 Locked    |

New players start with:
- 0 CloudCoins
- Soldier unlocked
- Marine locked

## ⚡ Quick Test

1. Open Menu scene
2. Press Play
3. Add `ShopSystemDebugHelper` to any GameObject
4. Right-click component → "Add 1000 CloudCoins"
5. Check Console for confirmation

## 🎨 UI Structure

```
Main Menu
    └─► SHOP button
           └─► Opens UIShopView
                  ├─► Shows CloudCoins balance
                  └─► Lists all agents
                         ├─► Soldier (OWNED/SELECTED)
                         └─► Marine (BUY - 500 coins)
```

## 📁 Files Location

All scripts in `/Assets/Scripts/`:
- `CloudCoinSystem.cs` - Currency
- `ShopSystem.cs` - Purchases
- `CloudCoinReward.cs` - Test helper
- `ShopSystemDebugHelper.cs` - Debug tools

All UI scripts in `/Assets/TPSBR/Scripts/UI/`:
- `MenuViews/UIShopView.cs` - Shop view
- `Widgets/UIShopItem.cs` - Shop item widget

Editor tools in `/Assets/Editor/`:
- `ShopUICreator.cs` - UI creation tool
- `ShopSystemSetupHelper.cs` - Settings tool

## 🎉 You're Ready!

Open **`SHOP_SYSTEM_COMPLETE.md`** and follow the 5-minute Quick Start guide!

---

**Need help?** All documentation is in `/Assets/Scripts/` folder.

# 🎮 Node Dialogue Manager for Unity

<div align="center">

![Unity Version](https://img.shields.io/badge/Unity-6.0%2B-blue?logo=unity)
![Version](https://img.shields.io/badge/Version-0.1.1-orange)
![License](https://img.shields.io/badge/License-MIT-green)
![Status](https://img.shields.io/badge/Status-Active-success)
![Platform](https://img.shields.io/badge/Platform-Universal-lightgrey)

**A powerful visual node-based dialogue system for Unity 6+**

*Create complex conversations, conditional logic, and game state management with an intuitive graph editor*

[Installation](#-installation) • [Quick Start](#-quick-start) • [Documentation](#-documentation) • [Features](#-features) • [License](#-license)

</div>

---

## 📖 Overview

**Node Dialogue Manager** is a professional-grade dialogue system built with modern Unity architecture. Leveraging **ScriptableObjects** for data persistence and **UI Toolkit/GraphView** for the editor, it provides a seamless workflow for creating branching narratives, conditional logic, and interactive conversations.

Perfect for RPGs, visual novels, adventure games, and any project requiring sophisticated dialogue systems.

---

## ✨ Key Features

### 🎨 **Visual Node Editor**
- Intuitive drag-and-drop interface for creating dialogue flows
- Real-time connection visualization
- Right-click context menu for quick node creation
- Clean, modern UI inspired by Shader Graph and Blueprint

### 📦 **ScriptableObject Architecture**
- Dialogue saved as reusable `DialogueAsset` files
- Easy integration with existing Unity workflows
- Project-wide asset management
- Version control friendly

### 🎯 **Blackboard Variable System**
Define and manipulate local variables for each dialogue:
- **Bool** - Boolean flags and switches
- **Int** - Counters, inventory quantities, stats
- **Float** - Timers, percentages, values
- **String** - Names, tags, custom data

### 💎 **Conditional Logic**

#### Branch Nodes (If/Else)
Create sophisticated branching paths based on:
- Variable states (bool, int, float comparisons)
- Custom conditions via `BaseCondition` classes
- True/False output ports for clear flow visualization

#### Conditional Options
Show or hide player choices dynamically:
```csharp
// Example: Only show "Intimidate" option if player has high strength
OptionNodeData option = new OptionNodeData();
option.AddCondition(new IntCondition("Strength", ComparisonType.GreaterThan, 15));
```

### ⚡ **Node Actions System**
Execute gameplay logic directly from dialogue nodes:
- **SetBoolAction** - Modify boolean variables
- **SetIntAction** - Update integer values
- **SetFloatAction** - Change float variables
- **SetStringAction** - Store text data
- **Custom Actions** - Extend with your own `BaseAction` classes

Perfect for:
- 🎭 Triggering cutscenes
- 🔓 Unlocking content
- 📊 Updating quest states
- 🎵 Playing audio/animations
- 🎮 Affecting game mechanics

### 🧩 **Essential Node Types**

| Node | Icon | Description |
|------|------|-------------|
| **START** | ▶️ | Entry point of the dialogue graph |
| **Speech** | 💬 | Character dialogue lines |
| **Option** | ❓ | Player choice branches |
| **Branch** | 💎 | Conditional if/else logic |

### 🛠️ **Developer Productivity**

#### Full Undo/Redo Support
- Node creation/deletion
- Connection changes
- Position adjustments
- Inspector edits
- Robust Command Pattern implementation

#### Search Window
- Quick node creation via right-click menu
- Drag from output port to spawn connected nodes
- Keyboard shortcuts for common operations

#### Custom Inspector
- Clean, focused interface
- Hides internal data (GUIDs, serialization details)
- Context-sensitive property display
- Inline action/condition editors

### 🎬 **Runtime Ready**

#### Core Components
- **DialogueRunner** - Executes dialogue logic
- **ConversationManager** - Manages conversation state
- **DialogueUIManager** - Handles UI display and input
- **TypewriterEffect** - Animated text reveal

#### Variable Persistence
- Blackboard state persists within game session
- Player choices have lasting consequences
- Reset variables between dialogues or maintain state

---

## 📥 Installation

### Option 1: Unity Package Manager (Recommended)

1. Open Unity and go to **Window > Package Manager**
2. Click the **+** button (top-left corner)
3. Select **"Add package from git URL..."**
4. Paste the following URL:
```
https://github.com/carlosbobao/node-dialogue-manager-unity.git
```
5. Click **Add**

### Option 2: Manual Installation

1. Clone or download this repository
2. Copy the package folder to your project's `Packages` directory
3. Unity will automatically detect and import the package

### Option 3: manifest.json

Add this line to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.carlosbobao.nodedialoguemanagerunity": "https://github.com/carlosbobao/node-dialogue-manager-unity.git"
  }
}
```

---

## 🚀 Quick Start

### 1. Create a Dialogue Asset
Right-click in your Project window:
```
Create > Dialogue System > Dialogue Asset
```

### 2. Open the Dialogue Editor
```
Window > Dialogue System > Dialogue Editor
```

### 3. Load Your Asset
- Drag your `DialogueAsset` onto the **"No Asset Loaded"** field in the toolbar, or
- Select the asset in Project view and click **Load** in the toolbar

### 4. Build Your Dialogue

#### Creating Nodes
- **Right-click** in the graph to open the search window
- **Drag** from an output port to create connected nodes
- **Use shortcuts** for quick creation

#### The START Node
Every dialogue begins with a **▶ START** node. This is your entry point.

#### Adding Speech
1. Create a **💬 Speech** node
2. Connect START's output to Speech's input
3. In the Inspector, set:
   - Character Name
   - Dialogue Text
   - Audio Clip (optional)

#### Player Choices
1. Create an **❓ Option** node after a Speech
2. Add multiple Option nodes for different choices
3. Each option connects to different dialogue branches

#### Conditional Logic
1. Create a **💎 Branch** node
2. In Inspector, add Conditions:
   - Example: `BoolCondition` checking if "HasKey" is true
3. Connect **True** port to one path, **False** to another

### 5. Configure Variables

#### Using the Blackboard
1. With asset loaded, the **BLACKBOARD** panel appears on the right
2. Click **+ Add Variable**
3. Configure:
   - Name (e.g., "HasKey")
   - Type (Bool/Int/Float/String)
   - Default Value

#### Setting Variables from Nodes
In any node's Inspector:
1. Expand **Actions** section
2. Click **+ Add Action**
3. Choose action type (e.g., `SetBoolAction`)
4. Configure:
   - Variable Name: "HasKey"
   - Value: true

### 6. Setup Your Scene

#### Create UI Document
1. Add UI Document to your scene
2. Assign UXML and USS from package samples
3. Add `DialogueUIManager` component

#### Create Dialogue Runner
1. Create empty GameObject (e.g., "GameManager")
2. Add `DialogueRunner` component
3. In Inspector, assign:
   - Your `DialogueUIManager`
   - Your `DialogueAsset`

### 7. Test It!
- Enter **Play Mode**
- Click **"▶ Start Dialogue (Debug)"** on DialogueRunner, or
- Call `DialogueRunner.StartDialogue(yourAsset)` from your scripts

---

## 📚 Documentation

### Node Types Reference

#### START Node ▶️
- **Purpose**: Entry point of dialogue
- **Ports**: 1 output
- **Usage**: Only one per graph, cannot be deleted

#### Speech Node 💬
- **Purpose**: Display character dialogue
- **Properties**:
  - Character Name
  - Dialogue Text
  - Audio Clip
  - Character Portrait (optional)
- **Actions**: Execute on node enter
- **Ports**: 1 input, 1 output

#### Option Node ❓
- **Purpose**: Player choice
- **Properties**:
  - Option Text
  - Conditions (show/hide logic)
- **Ports**: 1 input, 1 output per option

#### Branch Node 💎
- **Purpose**: Conditional branching
- **Properties**:
  - Conditions List
- **Ports**: 1 input, 2 outputs (True/False)

### Creating Custom Actions

```csharp
using DialogueSystem.Runtime.Actions;

public class PlaySoundAction : BaseAction
{
    public AudioClip clip;
    
    public override void Execute(ConversationManager manager)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }
}
```

### Creating Custom Conditions

```csharp
using DialogueSystem.Runtime.Conditions;

public class InventoryCondition : BaseCondition
{
    public string itemName;
    public int requiredAmount;
    
    public override bool Evaluate(ConversationManager manager)
    {
        // Your inventory check logic
        return InventorySystem.HasItem(itemName, requiredAmount);
    }
}
```

---

## 🎯 Advanced Usage

### Variable Scoping
Variables in the Blackboard are **local to each DialogueAsset**. To share data:
```csharp
// Access blackboard from code
var blackboard = dialogueAsset.blackboardData;
bool hasKey = blackboard.GetBool("HasKey");
blackboard.SetInt("Gold", 100);
```

### Multiple Dialogues
```csharp
public class NPCController : MonoBehaviour
{
    [SerializeField] private DialogueAsset greetingDialogue;
    [SerializeField] private DialogueAsset questDialogue;
    [SerializeField] private DialogueRunner runner;
    
    public void TalkToNPC()
    {
        bool questActive = QuestManager.IsQuestActive("MainQuest");
        runner.StartDialogue(questActive ? questDialogue : greetingDialogue);
    }
}
```

### Event Integration
Use Actions to trigger UnityEvents:
```csharp
public class TriggerEventAction : BaseAction
{
    public UnityEvent onExecute;
    
    public override void Execute(ConversationManager manager)
    {
        onExecute?.Invoke();
    }
}
```

---

## 🎨 Customization

### UI Styling
The dialogue UI uses **UI Toolkit (USS)**. Customize appearance by editing:
```
Runtime/UI/DialogueUI.uss
```

### Editor Theme
Modify node colors and styles in:
```
Editor/Resources/USS/DialogueEditorStyles.uss
```

---

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📝 Changelog

See [CHANGELOG.md](CHANGELOG.md) for detailed version history.

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE.md](LICENSE.md) file for details.

---

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/carlosbobao/node-dialogue-manager-unity/issues)
- **Discussions**: [GitHub Discussions](https://github.com/carlosbobao/node-dialogue-manager-unity/discussions)
- **Documentation**: *(Coming Soon)*

---

## 🌟 Acknowledgments

Built with ❤️ using:
- Unity UI Toolkit
- GraphView API
- ScriptableObject architecture

Inspired by modern node-based editors like Shader Graph, Blueprint, and Yarn Spinner.

---

<div align="center">

**⭐ If you find this useful, please consider starring the repository!**

Made by [chspDEV](https://github.com/chspDEV)

</div>

# Campfire Chronicles - Development Repository

## 🔥 Overview

**Campfire Chronicles** is an innovative narrative-driven game that leverages advanced AI technologies to deliver personalized storytelling experiences. Players create unique characters whose choices influence a dynamically generated 5-chapter story, complete with synchronized voice narration and immersive visuals.

Unlike traditional games with predetermined narratives, Campfire Chronicles generates unique stories for each player based on their character creation choices, creating infinite replayability and truly personalized gaming experiences.

## ✨ Features

- **AI-Powered Story Generation**: Uses Google's Gemini 2.5 Flash API to create personalized narratives
- **Professional Voice Narration**: High-quality text-to-speech via Murf AI API with synchronized audio
- **Dynamic Character Evolution**: Visual character progression through chapter transitions
- **Robust Input System**: Advanced input handling including Alt+Tab behavior and pause management
- **Seamless Media Integration**: Unskippable videos with interactive prompts and smooth transitions
- **Secure API Management**: Dynamic API key fetching via Google Sheets with user override capability
- **Modular Architecture**: Clean, extensible codebase designed for easy expansion and maintenance

## 🚀 Getting Started

### Prerequisites

- **Unity 2022.3.54f1 LTS**
- **API Access**: Gemini and Murf AI APIs (keys can be managed via Google Sheets or user settings)
- **Platform**: Windows, macOS, or Linux for development

### Installation

1. **Clone the repository**
```bash
git clone https://github.com/yourusername/campfire-chronicles-dev.git
cd campfire-chronicles-dev
```

3. **Open in Unity**
- Launch Unity Hub
- Click "Add" and select the project folder
- Open with Unity 2022.3.54f1 LTS

3. **Configure API Keys**
- Option A: Set up Google Sheets with API keys in cells A1 (Gemini) and A2 (Murf)
- Option B: Enter API keys directly in the in-game settings panel

4. **Assign Assets**
- Configure video clips, character evolution images, and UI elements in the inspector
- Ensure all scene references are properly linked

5. **Build and Run**
- Select your target platform in Build Settings
- Build and test the project

## 🎮 Usage

### Player Experience
- **Character Creation**: Define your protagonist through personality, background, skills, and goals
- **Dynamic Storytelling**: Experience a unique 5-chapter narrative generated specifically for your character
- **Interactive Controls**: Use skip and continue prompts to control story pacing
- **Immersive Audio**: Enjoy synchronized voice narration that matches the generated text
- **Visual Evolution**: Watch your character evolve through beautifully timed visual transitions

### Developer Features
- **Real-time API Integration**: Live story generation and voice synthesis
- **Fallback Systems**: Robust offline functionality with pre-written content
- **Debug Logging**: Comprehensive logging for troubleshooting and development
- **Modular Components**: Easy to modify and extend individual systems

## 📁 Project Structure

### Core Systems
- **`StoryDisplayManager.cs`**: Central hub for story text, audio synchronization, and user interaction
- **`UnskippableVideoPlayer.cs`**: Handles video playback, pause/resume, and input during media sequences
- **`ChapterTransition.cs`**: Controls chapter progression, visual transitions, and character evolution triggers
- **`PauseManager.cs`**: Manages game pause state, Alt+Tab behavior, and UI visibility
- **`CharacterEvolutionManager.cs`**: Handles character evolution images and progression visuals

### AI Integration
- **`GoogleSheetsKeyManager.cs`**: Secure API key fetching and management system
- **`StoryGenerator.cs`**: AI story generation, content processing, and fallback management
- **`EnhancedSettingsManager.cs`**: Settings UI, API key validation, and user preferences

### Supporting Systems
- **Character Creation**: Dynamic character building with choice-driven narrative influence
- **Audio Management**: Voice synthesis integration with caching and performance optimization
- **Input Handling**: Robust input detection with Alt+Tab protection and state management

## 🛠️ Development Setup

### Key Configuration Points
1. **API Keys**: Configure in `GoogleSheetsKeyManager` or via settings panel
2. **Media Assets**: Assign video clips and character images in respective managers
3. **UI References**: Ensure all UI elements are properly linked in the inspector
4. **Audio Setup**: Configure AudioMixer groups for proper audio management

## 📋 Development Roadmap



## 🐛 Known Issues

- Audio may occasionally desync on very slow systems
- Alt+Tab behavior may vary slightly across different operating systems
- API rate limiting may affect story generation speed during peak usage


## 🎯 Credits

- **Development and Art**: Aayush Beura
- **Storyline**: Ashmit Mandal
- **AI Integration**: Google Gemini 2.5 Flash API, Murf AI API
- **Framework**: Unity Technologies


## 🔗 Related Repositories

- **Executable Release**: [Campfire Chronicles - Releases](https://github.com/AayushBeura/the-campfire-chronicles-user)
- **Detailed Post**: [Dev.to Post]([https://github.com/yourusername/campfire-chronicles-docs](https://dev.to/aayushbeura04/the-campfire-chronicles-how-we-created-dynamic-storytelling-with-gemini-and-murf-tts-apis-2j7n))

**Note**: This is the development repository containing source code and Unity project files. For pre-built executables and releases, please visit the linked releases repository.

*Built with ❤️ using Unity and cutting-edge AI technologies*

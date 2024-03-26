# OpenBeepoBox
A modular VTuber streaming assistant toolkit in Godot

The addons availble in the tookit are:
- BeepoBits - The core functionality of the toolkit, has to be always included.
- BeepoAvatars - Custom scripts and nodes for running PNG/Inochi/VRM avatars.
- BeepoTwitch - Twitch integration for retrieving events such as follows, subscriptions, raids etc.
- BeepoFeatures - Extra effects you can apply to avatars compatible with BeepoAvatars.

# Requirements
Most addons in this repository are optional, except for BeepoBits which is required for all other addons to work. You always have to include the BeepoBits addon when working with this toolkit.

# Installation
1. Download the addons of interest to you and put them into the `addons` folder in your godot project. 
2. Enable the BeepoBits and any Addon you're using in the `project settings -> plugins` configuration.

# Credits
The [V-Sekai team](https://v-sekai.org/about) for making the VRM importer for Godot
ExpiredPopsicle for their incredibly generous help with Twitch integration and VMC support.
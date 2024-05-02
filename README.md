# OpenBeepoBox
A modular VTuber streaming assistant toolkit in Godot

The addons availble in the tookit are:
- BeepoCore - The core functionality of the toolkit, has to be always included.
- BeepoAvatarPNG - Custom scripts and nodes for running PNG avatars.
- BeepoAvatarVRM - Custom scripts and nodes for running VRM avatars.
- BeepoTracking - A VMC protocol receiver for tracking data. Can be used with trackers such as VSeeFace to animate the avatars.
- BeepoTwitch - Twitch integration for retrieving events such as follows, subscriptions, raids etc.

# Requirements
 - Godot .NET v4.2+
 - BeepoCore is required for all addons to work.

 For BeepoAvatarVRM you will also need the [VRM and Godot-MToon-Shader addons.](https://github.com/V-Sekai/godot-vrm)

# Installation
1. Download the addons of interest to you and put them into the `addons` folder in your godot project. 
2. Build the project once.
3. Enable BeepoCore and any Addon you're using in the `project settings -> plugins` configuration.

# Troubleshooting
Having trouble using BeepoBits? Checkout the [troubleshooting](https://github.com/FixItFreb/BeepoBits/wiki/Troubleshooting) page on the wiki.

# Credits
The [V-Sekai team](https://v-sekai.org/about) for making the VRM importer for Godot
ExpiredPopsicle for their incredibly generous help with Twitch integration and VMC support.
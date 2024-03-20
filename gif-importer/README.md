# Godot GIF importer
Plugin for [Godot Engine](https://godotengine.org/) to import GIF image as AnimatedTexture

## Installation

Download or clone this repository and copy the contents of the `addons` folder to your own project's `addons` folder.

Then enable the plugin on the Project Settings. All your project's GIF assets should now appear in the Godot's asset browser as AnimatedTextures.

## Credits

This little script uses [godot-gif-lzw](https://github.com/jegor377/godot-gif-lzw) to decompress images, and is entirely based on the content of this [website](http://www.matthewflickinger.com/lab/whatsinagif/bits_and_bytes.asp).

The author of godot-gif-lzw also developped a [little godot script](https://github.com/jegor377/godot-gdgifexporter) to compress and export GIF images.

## License

[MIT License](LICENSE). Copyright (c) 2021 Vincent Bousquet.

@tool
extends EditorPlugin

var import_sprite_frames_plugin
var import_animated_texture_plugin

func _enter_tree():
	import_sprite_frames_plugin = preload("GIF2SpriteFramesPlugin.gd").new()
	add_import_plugin(import_sprite_frames_plugin)
	import_animated_texture_plugin = preload("GIF2AnimatedTexturePlugin.gd").new()
	add_import_plugin(import_animated_texture_plugin)

func _exit_tree():
	remove_import_plugin(import_sprite_frames_plugin)
	import_sprite_frames_plugin = null
	remove_import_plugin(import_animated_texture_plugin)
	import_animated_texture_plugin = null

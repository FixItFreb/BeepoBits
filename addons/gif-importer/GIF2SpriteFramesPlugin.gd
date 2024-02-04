# Derived from https://github.com/jegor377/godot-gdgifexporter

@tool
extends EditorImportPlugin

func _get_importer_name():
	return "gif.animated.texture.plugin"

func _get_visible_name():
	return "Sprite Frames"

func _get_recognized_extensions():
	return ["gif"]

func _get_save_extension():
	return "tres"

func _get_resource_type():
	return "SpriteFrames"

func _get_preset_count():
	return 1

func _get_import_order() -> int:
	return 0

func _get_preset_name(i):
	return "Default"

func _get_option_visibility(path: String, option_name: StringName, options: Dictionary) -> bool:
	return true

func _get_priority() -> float:
	return 1.0

func _get_import_options(i, num):
	return [
		{"name": "Filter", "default_value": false},
		{"name": "MipMaps", "default_value": false}
	]

func _import(source_file, save_path, options, platform_variants, gen_files):
	var reader = GifReader.new()
	reader.filter = options["Filter"]
	reader.mipmaps = options["MipMaps"]
	var tex = reader.read(source_file)
	if tex == null:
		return FAILED
	var filename = save_path + "." + _get_save_extension()
	var sf = SpriteFrames.new()
	var minLength = 1000
	var maxLength = 0
	for i in range(0, tex.frames):
		sf.add_frame("default", tex.get_frame_texture(i))
		var length = tex.get_frame_duration(i)
		minLength = min(minLength, length)
		maxLength = min(maxLength, length)
	sf.set_animation_speed("default", 2.0 / (minLength + maxLength))
	return ResourceSaver.save(sf, filename)

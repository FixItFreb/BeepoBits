extends Node

var effect : AudioEffectCapture


func _ready() -> void:
	var bus_id = AudioServer.get_bus_index("Mic")
	effect = AudioServer.get_bus_effect(bus_id, 0)
	print(effect)
	print(AudioServer.get_input_device_list())
	#AudioServer.input_device = "Microphone (HyperX Quadcast)"


func _process(delta: float) -> void:
	var sample_size = effect.get_frames_available()
	var values : PackedVector2Array = effect.get_buffer(sample_size)
	
	var sum = Vector2.ZERO
	if (values.size() > 0):
		for v in values:
			sum += v.abs();
		sum /= values.size()
	print(sum)

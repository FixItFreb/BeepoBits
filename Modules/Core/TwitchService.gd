extends Node
class_name TwitchService

# -----------------------------------------------------------------------------
# Settings

# Client ID for the twitch application. Found here:
#
#   https://dev.twitch.tv/console/apps
#
@export var twitch_client_id : String = ""

# NOTE: Whatever setting you put here will be clobbered by whatever is in the
# saved configuration file, so if you're modifying it directly (through the
# editor) instead of relying on saved credentials, you'll have to make sure the
# saved credentials file gets cleared out when you need a new token.
@export var twitch_oauth : String = ""

# To be filled out per-user.
@export var twitch_username : String = ""

# Location to store config once it's set, so you don't have to go through the
# token generation flow all the time.
@export var twitch_config_path : String = "user://twitch_config.ini"

# Automatically save credentials on startup and any time set_twitch_credentials
# is called.
@export var auto_save_credentials : bool = true

# Automatically load credentials when starting.
@export var auto_load_credentials : bool = true

# -----------------------------------------------------------------------------
# Signals

# Emitted when a user uses bits to cheer.
signal handle_channel_chat_message(
	cheerer_username, cheerer_display_name, message, bits_count)

# Emitted when a user redeems a channel point redeem.
signal handle_channel_points_redeem(
	redeemer_username, redeemer_display_name, redeem_title, user_input)

# Emitted when another user raids your channel.
signal handle_channel_raid(
	raider_username, raider_display_name, raid_user_count)
	
# -----------------------------------------------------------------------------
# Constants

# Pubsub connection target.
var _twitch_service_url = "wss://pubsub-edge.twitch.tv"

# IRC (over websocket) connection target.
var _twicth_irc_url = "wss://irc-ws.chat.twitch.tv"

# Twitch user data endpoint. We'll use this to fetch a user ID based on the
# username.
var _twitch_users_endpoint = "https://api.twitch.tv/helix/users"

var _twitch_redirect_port = 8080

# -----------------------------------------------------------------------------
# Oauth flow

var _oauth_in_process = false
var _oauth_tcpserver : TCPServer = null

# TODO: Make into an array, so we can support multiple incoming connections,
# just in case the browser tries to read our favicon or something.
var _oauth_streampeertcp : StreamPeerTCP = null
var _oauth_streampeertcp_inputbuffer : String = ""

func _stop_oauth_process():

	if _oauth_tcpserver:
		_oauth_tcpserver.stop()
		_oauth_tcpserver = null
	
	if _oauth_streampeertcp:
		_oauth_streampeertcp.disconnect_from_host()
		_oauth_streampeertcp = null

	_oauth_in_process = false
	_oauth_streampeertcp_inputbuffer = ""

func _oauth_send_page_data(peer, data):
	var http_response = "\r\n".join([
		"HTTP/1.1 200 OK",
		"Content-Type: text/html; charset=utf-8",
		"Content-Length: " + String.num_int64(len(data)),
		"Connection: close",
		"Cache-Control: max-age=0",
		"", ""])
	var full_response = http_response + data + "\n\n\n\n\n"
	var response_ascii = full_response.to_ascii_buffer()
	peer.put_data(response_ascii)
					
func _poll_oauth_server():

	if not _oauth_in_process:
		return

	# Accept incoming connections.
	if _oauth_tcpserver:
		if _oauth_tcpserver.is_connection_available():
			_oauth_streampeertcp = _oauth_tcpserver.take_connection()
	
	# Add any new incoming bytes to our input buffer.
	if _oauth_streampeertcp:
		while _oauth_streampeertcp.get_available_bytes():
			var incoming_byte = _oauth_streampeertcp.get_utf8_string(1)
			if incoming_byte != "\r":
				_oauth_streampeertcp_inputbuffer += incoming_byte

	# Only act on stuff once we have two newlines at the end of a request.	
	if _oauth_streampeertcp_inputbuffer.ends_with("\n\n"):
	
		# For each line...
		while _oauth_streampeertcp_inputbuffer.contains("\n"):
			
			# Take the line and pop it out of the buffer.
			var get_line = _oauth_streampeertcp_inputbuffer.split("\n", true)[0]
			_oauth_streampeertcp_inputbuffer = _oauth_streampeertcp_inputbuffer.substr(len(get_line) + 1)
			
			# All we care about here is the GET line.
			if get_line.begins_with("GET "):
				
				# Split "GET <path> HTTP/1.1" into "GET", <path>, and
				# "HTTP/1.1".
				var get_line_parts = get_line.split(" ")
				var http_get_path = get_line_parts[1]
				
				# If we get the root path without the arguments, then it means
				# that Twitch has stuffed the access token into the fragment
				# (after the #). Send a redirect page to read that and give it
				# to us in a GET request.
				if http_get_path == "/":	
					
					# Response page: Just a Javascript program to do a redirect
					# so we can get the access token into the a GET argument
					# instead of the fragment.
					var html_response = """
						<html><head></head><body><script>
							  var url_parts = String(window.location).split("#");
							  if(url_parts.length > 1) {
								  var redirect_url = url_parts[0] + "?" + url_parts[1];
								  window.location = redirect_url;
							  }
						</script></body></html>
					"""

					# Send webpage and disconnect.
					_oauth_send_page_data(_oauth_streampeertcp, html_response)			
					_oauth_streampeertcp.disconnect_from_host()
					_oauth_streampeertcp = null
				
				# If the path has a '?' in it at all, then it's probably our
				# redirected page.
				elif http_get_path.contains("?"):
				
					var html_response = """
						<html><head></head><body>You may now close this window.</body></html>
					"""

					# Attempt to extract the access token from the GET data.
					var path_parts  = http_get_path.split("?")
					if len(path_parts) > 1:
						var parameters = path_parts[1]
						var arg_list = parameters.split("&")
						for arg in arg_list:
							var arg_parts = arg.split("=")
							if len(arg_parts) > 1:
								if arg_parts[0] == "access_token":
									twitch_oauth = arg_parts[1]

					# Send webpage and disconnect.
					_oauth_send_page_data(_oauth_streampeertcp, html_response)
					_oauth_streampeertcp.disconnect_from_host()
					_oauth_streampeertcp = null
					_stop_oauth_process()

func _start_oauth_process():
	
	_oauth_in_process = true
	
	# Kill any existing websocket server.
	if _oauth_tcpserver:
		_oauth_tcpserver.stop()
		_oauth_tcpserver = null

	# Fire up the new server.
	_oauth_tcpserver = TCPServer.new()	
	_oauth_tcpserver.listen(_twitch_redirect_port, "127.0.0.1")

	# Check client ID to make sure we aren't about to do something we'll regret.
	var ascii_twitch_id = twitch_client_id.to_ascii_buffer()
	for k in ascii_twitch_id:
		assert( \
			(k >= 65 and k <= 90) or \
			(k >= 97 and k <= 122) or \
			(k >= 48 and k <= 57))

	# Notes on scopes used in this URL:
	#   channel:read:redemptions - Needed for point redeems.
	#   chat:read                - Needed for reading chat (and raids?).
	#   bits:read                - Needed for reacting to bit donations.

	var oauth_url = "https://id.twitch.tv/oauth2/authorize?response_type=token&client_id=" + \
		twitch_client_id + \
		"&redirect_uri=http://localhost:" + \
		str(_twitch_redirect_port) + \
		"&scope=channel%3Aread%3Aredemptions%20chat%3Aread%20bits%3Aread"
	OS.shell_open(oauth_url)

# -----------------------------------------------------------------------------
# User ID fetch

var _twitch_user_id = -1
var _twitch_user_id_fetch_time_to_retry = 0.0
var _twitch_user_id_fetch_http_client = null

func _user_id_request_completed(
	_result: int, response_code: int,
	_headers: PackedStringArray,
	body: PackedByteArray):
	
	var parsed_result = JSON.parse_string(
		body.get_string_from_utf8())

	# If we get an authorization error, we need to re-do the oauth setup.
	if response_code == 401:
		_start_oauth_process()
		if _twitch_user_id_fetch_http_client:
			_twitch_user_id_fetch_http_client.queue_free()
			_twitch_user_id_fetch_http_client = null
		return

	# Get the user ID and login from the incoming Twitch data.
	_twitch_user_id = -1
	for user in parsed_result["data"]:
		_twitch_user_id = int(user["id"])
		set_twitch_credentials(user["login"], twitch_oauth)
		break

	# Clean up.
	if _twitch_user_id_fetch_http_client:
		_twitch_user_id_fetch_http_client.queue_free()
		_twitch_user_id_fetch_http_client = null

	_twitch_user_id_fetch_time_to_retry = 5.0

# Determine the user ID of the user who's authorized this.
func _fetch_user_id():

	if _twitch_user_id_fetch_http_client:
		# Request already in-flight.
		return

	_twitch_user_id_fetch_http_client = HTTPRequest.new()
	_twitch_user_id_fetch_http_client.set_name("temp_request")
	add_child(_twitch_user_id_fetch_http_client)
	_twitch_user_id_fetch_http_client.set_name("temp_request")
	_twitch_user_id_fetch_http_client.request_completed.connect(
		self._user_id_request_completed)

	var header_params = [
		"Authorization: Bearer " + twitch_oauth,
		"Client-Id: " + twitch_client_id
	]

	var err = _twitch_user_id_fetch_http_client.request(
		_twitch_users_endpoint,
		header_params)
		
	if err != OK:
		_twitch_user_id_fetch_http_client.queue_free()
		_twitch_user_id_fetch_http_client = null

	_twitch_user_id_fetch_time_to_retry = 5.0

func _update_user_id(delta):

	if _oauth_in_process:
		return

	# Check user ID. See if we need to fetch that. If we do, then we can't do
	# anything else until that's ready.
	if _twitch_user_id == -1:
		_twitch_user_id_fetch_time_to_retry -= delta
		if _twitch_user_id_fetch_time_to_retry < 0.0:
			_twitch_user_id_fetch_time_to_retry = 5.0 # Try every 5 seconds.
			_fetch_user_id()

# -----------------------------------------------------------------------------
# Pubsub

var _client_pubsub : WebSocketPeer = WebSocketPeer.new()
var _client_pubsub_time_to_reconnect = 0.0
var _client_pubsub_time_to_ping = 30.0

func _client_pubsub_fail_and_restart(_error_message):
	_client_pubsub_time_to_reconnect = 10.0

func _client_pubsub_handle_connection_closed(_peer_id : int):
	_client_pubsub_fail_and_restart("Connection closed")

func _client_pubsub_handle_connection_error(_was_clean = false):
	_client_pubsub_fail_and_restart("Connection closed with error")

func _client_pubsub_send_ping():

	# Send a ping! For funsies or something.
	var ping_json = {
		"type" : "PING",
	}
	var ping_data = JSON.stringify(ping_json)
	_client_pubsub.send_text(ping_data)
	
	print("pubsub ping!")

func _client_pubsub_handle_connection_established(_peer_id : int):

	# Send a ping! For funsies or something.
	_client_pubsub_send_ping()

	# Register for channel point redeems.
	var event_registration_json = {
		"type" : "LISTEN",
		"nonce" : "ChannelPoints",
		"data" : {
			"topics" : [
				"channel-points-channel-v1." + str(_twitch_user_id),
				"channel-bits-events-v1." + str(_twitch_user_id)
			],
			"auth_token" : twitch_oauth
		}
	}	
	var event_registration_data = JSON.stringify(event_registration_json)
	_client_pubsub.send_text(event_registration_data)

func _client_pubsub_handle_reward_redeemed(title, username, display_name, user_input):
	emit_signal("handle_channel_points_redeem",
		username, display_name, title, user_input)

func _client_pubsub_handle_message(_topic, message):

	if "type" in message.keys():
		if message["type"] == "reward-redeemed":
			var user_input = ""
			if "user_input" in message["data"]["redemption"]:
				user_input = message["data"]["redemption"]["user_input"]
		
			_client_pubsub_handle_reward_redeemed(
				message["data"]["redemption"]["reward"]["title"],
				message["data"]["redemption"]["user"]["login"],
				message["data"]["redemption"]["user"]["display_name"],
				user_input)
				
func _client_pubsub_handle_data_received():
	var result_str = _client_pubsub.get_packet().get_string_from_utf8()
	pubsub_inject_packet(result_str)

# Inject a packet to handle a pubsub message. This is used for both real and
# fake (testing) packets.
func pubsub_inject_packet(packet_text):
	var result_dict = JSON.parse_string(packet_text)
	var _result_indented = JSON.stringify(result_dict, "    ")

	if result_dict["type"] == "MESSAGE":
		_client_pubsub_handle_message(
			result_dict["data"]["topic"],
			JSON.parse_string(result_dict["data"]["message"]))

func _client_pubsub_connect_to_twitch():
	
	# If you hit this assert, it's because you never filled out the Twitch
	# client ID, which is specific to your application. If you want to find out
	# what it is for your app, you can find it in your app settings here:
	#
	# https://dev.twitch.tv/console/apps
	#
	assert(twitch_client_id != "")
	
	# Attempt connection.
	var err = _client_pubsub.connect_to_url(_twitch_service_url)
	if err != OK:
		_client_pubsub_fail_and_restart("Connection failed: " + str(err))
		return
	
	# Wait for the connection to be fully established.
	_client_pubsub.poll()
	while _client_pubsub.get_ready_state() == WebSocketPeer.STATE_CONNECTING:
		_client_pubsub.poll()
	
	# Handle failed connections.
	if _client_pubsub.get_ready_state() == WebSocketPeer.STATE_CLOSING:
		return
	if _client_pubsub.get_ready_state() == WebSocketPeer.STATE_CLOSED:
		return

	# Send subscription messages.
	_client_pubsub.poll()
	_client_pubsub_handle_connection_established(1)
	_client_pubsub.poll()

func _client_pubsub_update(delta):

	if _twitch_user_id == -1:
		return

	_client_pubsub.poll()

	var err = _client_pubsub.get_packet_error()
	if err != OK:
		print("ERROR!!!! ", err)

	while _client_pubsub.get_available_packet_count():
		_client_pubsub_handle_data_received()
		_client_irc.poll()

	# See if we need to reconnect.
	if _client_pubsub.get_ready_state() == WebSocketPeer.STATE_CLOSED:

		_client_pubsub_time_to_reconnect -= delta

		if _client_pubsub_time_to_reconnect < 0.0:

			# Reconnect to Twitch websocket.
			_client_pubsub_connect_to_twitch()

			# Whatever happens, set a default reconnect delay.
			_client_pubsub_time_to_reconnect = 20.0

	else:
		
		_client_pubsub_time_to_ping -= delta
		if _client_pubsub_time_to_ping < 0.0:
			_client_pubsub_time_to_ping = 30.0
			_client_pubsub_send_ping()

	_client_pubsub.poll()

# -----------------------------------------------------------------------------
# IRC

var _client_irc : WebSocketPeer = WebSocketPeer.new()
var _client_irc_time_to_reconnect = 0.0

func _client_irc_fail_and_restart(_error_message):
	_client_irc_time_to_reconnect = 10.0

func _client_irc_handle_connection_closed(_was_clean = false):
	_client_irc_fail_and_restart("Connection closed")

func _client_irc_handle_connection_error(_was_clean = false):
	_client_irc_fail_and_restart("Connection closed with error")

func _client_irc_send(message):
	_client_irc.send_text(message)

func _client_irc_handle_connection_established(_proto = ""):

	# Send IRC handshaking messages.
	_client_irc_send("CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands") # twitch.tv/tags twitch.tv/commands
	_client_irc_send("PASS oauth:" + twitch_oauth)
	_client_irc_send("NICK " + twitch_username)
	_client_irc_send("JOIN #" + twitch_username)
	
func _parse_irc_message(message):
	
	var split_message
	var output = {}
	output["tags"]    = {}
	output["prefix"]  = ""
	output["command"] = ""
	output["params"]  = []

	# Parse tags.
	if message.length() > 0:
		if message[0] == "@":
			split_message = message.split(" ", false, 1)
			var tags_str = split_message[0].substr(1)
			if split_message.size() > 1:
				message = split_message[1]
			else:
				message = ""
			var tags_pair_strs = tags_str.split(";")
			for tag_pair in tags_pair_strs:
				var tag_parts = tag_pair.split("=")
				output["tags"][tag_parts[0]] = tag_parts[1]

	# Parse prefix, and chop it off from the message if it's there.
	if message.length() > 0:
		if message[0] == ":":
			split_message = message.split(" ", false, 1)
			output["prefix"] = split_message[0].substr(1)
			if split_message.size() > 1:
				message = split_message[1]
			else:
				message = ""

	if output["prefix"].length() > 0:

		# Here are what I think are the three forms of prefix we might be
		# dealing with here:
		# - nick!user@host
		# - user@host (maybe?)
		# - host
		
		# Split on "!" to separate the nick from everything else. We might not
		# have a nick, but that's okay. We'll just leave the field blank.
		var prefix_nick_user = output["prefix"].split("!", true, 1)
		var nick
		var prefix_user_host
		if prefix_nick_user.size() > 1:
			nick = prefix_nick_user[0]
			prefix_user_host = prefix_nick_user[1]
		else:
			nick = ""
			prefix_user_host = prefix_nick_user[0]

		# Split the user@host by "@" to get a user and host. It may also just
		# be a host, so if we only have one result from this, assume it's a host
		# with no user (message directly from server, etc).
		var prefix_user_host_split = prefix_user_host.split("@", true, 1)
		var user
		var host
		if prefix_user_host_split.size() > 1:
			user = prefix_user_host_split[0]
			host = prefix_user_host_split[1]
		else:
			user = ""
			host = prefix_user_host_split[0]
			
		output["prefix_nick"] = nick
		output["prefix_host"] = host
		output["prefix_user"] = user

	# Parse command, and chop it off from the message if it's there.
	if message.length() > 0:
		split_message = message.split(" ", false, 1)
		output["command"] = split_message[0]
		if split_message.size() > 1:
			message = split_message[1]
		else:
			message = ""

	# Parse the parameters to the command.
	while message.length() > 0:
		if message[0] == ":":
			output["params"].append(message.substr(1))
			message = ""
		else:
			split_message = message.split(" ", false, 1)
			output["params"].append(split_message[0])
			if split_message.size() > 1:
				message = split_message[1]
			else:
				message = ""

	return output

func _client_irc_handle_data_received():
	var packet_text = _client_irc.get_packet().get_string_from_utf8()
	irc_inject_packet(packet_text)

func irc_inject_packet(packet_text):

	# This might be multiple messages, separated by CRLF, so split it up.
	var irc_messages = packet_text.split("\r\n")

	for message in irc_messages:
		if message.length():
#			print("IRC: " + message)
			var parsed_message = _parse_irc_message(message)

			# Just respond to pings right here.
			if parsed_message["command"].to_lower() == "ping":
				_client_irc_send("PONG :" + parsed_message["params"][0])

			# Raids and other stuff that comes in by USERNOTICE.
			if parsed_message["command"].to_lower() == "usernotice":
				if "msg-id" in parsed_message["tags"]:
#					print("Message ID: ", parsed_message["tags"]["msg-id"])
					if parsed_message["tags"]["msg-id"] == "raid":

						# Looks like we got an actual raid! Fire off the signal.
						emit_signal(
							"handle_channel_raid",
							parsed_message["tags"]["msg-param-login"],
							parsed_message["tags"]["msg-param-displayName"],
							parsed_message["tags"]["msg-param-viewerCount"])

			# Handle incoming messages, including bit cheers.
			if parsed_message["command"].to_lower() == "privmsg":

				var message_text = ""
				if parsed_message["params"].size() > 1:
					message_text = parsed_message["params"][1]

				# Make sure this is meant for us (for the channel).
				if parsed_message["params"].size() > 0:
					if parsed_message["params"][0] == "#" + twitch_username:

						# Bit cheer message?
						if "bits" in parsed_message["tags"]:
							emit_signal(
								"handle_channel_chat_message",
								parsed_message["prefix_user"], # FIXME: User or nick?
								parsed_message["tags"]["display-name"],
								message_text,
								int(parsed_message["tags"]["bits"]))
						else:
							emit_signal(
								"handle_channel_chat_message",
								parsed_message["prefix_user"], # FIXME: User or nick?
								parsed_message["tags"]["display-name"],
								message_text,
								0)

func _client_irc_connect_to_twitch():
	
	# If you hit this assert, it's because you never filled out the Twitch
	# client ID, which is specific to your application. If you want to find out
	# what it is for your app, you can find it in your app settings here:
	#
	# https://dev.twitch.tv/console/apps
	#
	assert(twitch_client_id != "")
	
	var err = _client_irc.connect_to_url(_twicth_irc_url)
	if err != OK:
		_client_irc_fail_and_restart("Connection failed: " + str(err))

	_client_irc.poll()
	while _client_irc.get_ready_state() == WebSocketPeer.STATE_CONNECTING:
		_client_irc.poll()

	if _client_irc.get_ready_state() == WebSocketPeer.STATE_CLOSED || \
		_client_irc.get_ready_state() == WebSocketPeer.STATE_CLOSING:
		
		return
	
	_client_irc_handle_connection_established("")

func _client_irc_update(delta):

	if _twitch_user_id == -1:
		return

	_client_irc.poll()
	while _client_irc.get_available_packet_count():
		_client_irc_handle_data_received()
		_client_irc.poll()

	# See if we need to reconnect.
	if _client_irc.get_ready_state() == WebSocketPeer.STATE_CLOSED:
		
		_client_irc_time_to_reconnect -= delta

		if _client_irc_time_to_reconnect < 0.0:

			# Reconnect to Twitch websocket.
			_client_irc_connect_to_twitch()

			# Whatever happens, set a default reconnect delay.
			_client_irc_time_to_reconnect = 20.0

# -----------------------------------------------------------------------------
# Config Management

func load_config():

	if twitch_config_path == "":
		return

	var config = ConfigFile.new()
	var err = config.load(twitch_config_path)
	if err != OK:
		return
	
	# Load the values, but default to whatever was there (export values that may
	# have been set in the editor.)
	if config.has_section_key("twitch", "twitch_username"):
		twitch_username = config.get_value("twitch", "twitch_username", twitch_username)
	if config.has_section_key("twitch", "twitch_oauth_token"):
		twitch_oauth = config.get_value("twitch", "twitch_oauth_token", twitch_oauth)

func save_config():

	if twitch_config_path == "":
		return

	var config = ConfigFile.new()
	config.set_value("twitch", "twitch_username", twitch_username)
	config.set_value("twitch", "twitch_oauth_token", twitch_oauth)
	config.save(twitch_config_path)

func set_twitch_credentials(username, oauth_token):

	if username and username != "":
		twitch_username = username
	if oauth_token and oauth_token != "":
		twitch_oauth = oauth_token

	if auto_save_credentials:
		save_config()

# -----------------------------------------------------------------------------
# Normal Node entry points

func _ready():
	
	if auto_load_credentials:
		load_config()

	if auto_save_credentials:
		save_config()

func _process(delta):

	# Check user ID.
	_update_user_id(delta)

	# Update Pubsub.
	_client_pubsub_update(delta)

	# Update IRC.
	_client_irc_update(delta)

	# Poll oauth.
	_poll_oauth_server()






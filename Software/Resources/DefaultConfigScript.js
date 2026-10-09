// AddScriptChannel(c, function(e, b, t, a) { ... })
//   Runs the function for channel c (0-7) every 100 ms with:
//     e  encoder steps since the last call (+ = clockwise)
//     b  button count, +1 per press and per release (odd = held);
//        toggle(b, start) turns it into an on/off switch starting at start
//     t  touch sensor reading
//     a  ambient light reading
//   The function returns {text, leds}:
//     text  LCD text; a leading ` switches to the 7-segment font
//     leds  21 values 0-255: the 20 LEDs, then the LCD backlight;
//           BlankLeds(backlight) gives all LEDs off
//
// Ready-made channels:
//   AddVolumeChannel(c, label, exe)  volume of programs whose exe name ends with exe
//   AddTimeChannel(c)                clock; button toggles seconds
//   AddTimerChannel(c, seconds)      countdown, started by the button
//   AddTouchChannel(c)               touch reading; button toggles it
//   AddAmbientChannel(c)             ambient reading; button toggles it
//   AddInvocationCounterChannel(c)   counts calls (debugging)
//
// All of these except AddVolumeChannel return the channel, which has:
//   SetPeriod(ms)  run the function every ms instead (0 = every USB packet)

AddVolumeChannel(0, "Chrome", "chrome.exe");
AddVolumeChannel(1, "Rocket", "RocketLeague.exe");
AddVolumeChannel(2, "Teamspeak", "ts3client_win64.exe");
AddTimeChannel(3);
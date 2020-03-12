// Basic Documentation:
//  
//   AddScriptChannel(c, function(e, b, t, a) ... end)
//     Puts channel c in control of the provided callback function. Arguments
//     of the callback are:
//     - ch: A channel display object that allows the callback to display 
//           things on the LCD or LED.
//     - e: Encoder count.
//     - b: Button state -- incremented per push and release. So bit 0 is set
//          if the button is currently depressed; bit 1 is set if the button
//          has been pressed an odd number of times. The toggle() function is
//          a utility for implementing a toggle functionality using the value
//          of b.
//     - t: Touch sensor output. 
//     - a: Ambient light sensor output (logarithmic).
//  
//   AddVolumeChannel(c, label, fn)
//     Attaches channel c to the volume state of programs with fn in their
//     filenames. Argument label determines the label on the LCD screen.
//  
//   AddTimeChannel(c)
//     Makes channel c display the current system time. Pressing the button
//     toggles the LCD backlight.
//  
//   AddTimerChannel(c, duration_seconds)
//     Makes channel c a countdown timer that starts at duration_seconds.
//  
//   AddTouchChannel(c)
//     Makes channel c display the current touch sensor value. Pressing the
//     button toggles the display.
//  
//   AddAmbientChannel(c)
//     Makes channel c display the current ambient light sensor value. Pressing
//     the button toggles the display.
//  
//   AddInvocationCounterChannel(c)
//     For debugging purposes, makes channel c an invocation counter. An 
//     invocation counter displays the number of times an invocation counter
//     has been invoked.
//  
// All of the above functions except AddVolumeChannel return a ScriptChannel 
// object, which has this method:
//
//   SetPeriod(duration_ms)
//     Causes the channel handler to only be invoked every duration_ms, rather
//     than for every frame. Helps keep CPU usage down for channel that don't 
//     require high speed response.
//
// For example, the following will produce a counter that increments once per
// second:
//
//   AddInvocationCounterChannel(3).SetPeriod(1000)

AddVolumeChannel(0, "Chrome", "chrome.exe");
AddVolumeChannel(1, "Rocket", "RocketLeague.exe");
AddVolumeChannel(2, "Teamspeak", "ts3client_win64.exe");
AddTimeChannel(3);
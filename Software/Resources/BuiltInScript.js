// Built-in Script config. Provides a richer group of pre-defined channel types on 
// top of those provided by the C# code. See DefaultConfigScript.txt for more
// documentation.

function toggle(b, defaultt) {
	return (Math.floor((b+1)/2)+defaultt+1) % 2 == 0;
}

function ostime() {
	return (new Date()).getTime() / 1000;
}

function BlankLeds(backlight) {
	leds = [];
	for (var i = 0; i < 20; i++) {
		leds.push(0);
	}
	leds.push(backlight);
	return leds;
}

function AddScriptChannel(c, callback) {
	return AddScriptChannelInternal(c, host.del(ScriptChannelCallback, callback));
}

function AddTimeChannel(c) {
    return AddScriptChannel(c, function(e, b, t, a) {
      var d = new Date();
      var hr = d.getHours();
	  if (hr < 10) {
		hr = ' ' + hr;
	  }
      var min = d.getMinutes();
      if (min < 10) {
        min = '0' + min;
      }

      return {text: '`' + '```````````````````' + hr + '`:`' + min, leds: BlankLeds(toggle(b, 1) ? 128 : 0)};
    })/*.SetPeriod(1029)*/;
}

function AddTouchChannel(c) {
	return AddScriptChannel(c, function(e, b, t, a) {
		return {text: toggle(b, 0) ? "" + t : "", leds: BlankLeds(toggle(b, 0) ? 128 : 0)};
	});
}

function AddAmbientChannel(c) {
	return AddScriptChannel(c, function(e, b, t, a) { return {text: toggle(b, 0) ? ""+a : "", leds: BlankLeds(toggle(b, 0) ? 128 : 0) }; });
}

var invocationCount = 0;
function AddInvocationCounterChannel(c) {
	return AddScriptChannel(c, function(e, b, t, a) { invocationCount = invocationCount + 1; return {text: ""+invocationCount, leds: BlankLeds(128) }; });
}

var timerend = -1;
function AddTimerChannel(c, duration) {
	return AddScriptChannel(c, function(e, b, t, a) {
		if (timerend == -1) {
			if (b % 2 == 1) {
				timerend = ostime() + duration;
			}
		}
		if (timerend == -2) {
			if (b % 2 == 0) {
				timerend = -1;
			}
		}
		if (timerend != -1) {
			remaining = timerend - ostime();
			if (remaining < 0) {

				if (b % 2 == 1) {
					timerend = -2;
					return {text: "", leds: BlankLeds(0)};
				}
				if (remaining < -3600) {
					timerend = -2;
					return {text: "", leds: BlankLeds(0)};
				}
								
				var ccc = [0, 0, 0];
				var sel = Math.floor((ostime()*10) % 3);
				ccc[sel] = 255;


				return {text: "", leds: [ccc[1], ccc[2], ccc[0], ccc[1], ccc[2], ccc[0], ccc[1], ccc[2], ccc[0], ccc[1], ccc[2], ccc[0], ccc[1], ccc[2], ccc[0], ccc[1], ccc[2], ccc[0], ccc[1], ccc[2], ccc[0]]};
			}
			return {text: "" + (Math.ceil(remaining)), leds: BlankLeds(128)};
		}
		return {text: "", leds: BlankLeds(0)};
	});
}

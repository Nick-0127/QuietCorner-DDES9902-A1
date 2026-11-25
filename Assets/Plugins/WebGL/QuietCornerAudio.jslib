// Unity 6000.4.0a2 can send non-finite loop boundaries even for one-shot
// AudioSource playback. Web Audio rejects those values and halts the player.
// Zero is Web Audio's default: start at zero, end at the buffer's end.
// Preserve all finite boundaries, including custom loop regions.
mergeInto(LibraryManager.library, {
  JS_Sound_SetLoopPoints__deps: ['$WEBAudio'],
  JS_Sound_SetLoopPoints: function(channelInstance, loopStart, loopEnd) {
    if (WEBAudio.audioWebEnabled === 0) return;
    var channel = WEBAudio.audioInstances[channelInstance];
    if (!channel) return;
    channel.setLoopPoints(
      Number.isFinite(loopStart) ? loopStart : 0,
      Number.isFinite(loopEnd) ? loopEnd : 0
    );
  }
});

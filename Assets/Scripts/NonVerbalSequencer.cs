using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NonVerbalSequencer : SoundObjectSequencer {
		
	public void FixedUpdate() {
		if (!active || path.Count < 1)
			return;
		
		bool toggleplay = false;
		comet.transform.localPosition = path [nextPos];
		if (playtriggers.Count > 0) {
			if (nextPos == playtriggers [nextInOut])
				toggleplay = true;
			if (toggleplay) {
				playstate = !playstate;
				m_acts.playSound (playstate);
				nextInOut++;
				if (nextInOut == playtriggers.Count) {
					nextInOut--;
				}
			}
		}
		nextPos++;
		if (nextPos == path.Count) {
			active = false;
			if (isServer)
				IAAPlayer.localPlayer.CmdSoundObjectStartSequencer (netId);
		}

	}
}

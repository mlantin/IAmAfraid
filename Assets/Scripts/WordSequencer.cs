using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WordSequencer : SoundObjectSequencer {

	public void FixedUpdate() {
		if (!active || scrubs.Count <= 1)
			return;

		bool toggleplay = false;
		if (path.Count > 0) {
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
			if (playstate == true) {
				((WordActs)m_acts).setLocalGranOffset (scrubs [nextScrub]);
				nextScrub++;
			}
			nextPos++;
			if (nextPos == path.Count) {
				active = false;
				if (isServer)
					IAAPlayer.localPlayer.CmdSoundObjectStartSequencer (netId);
			}
		}
	}

}

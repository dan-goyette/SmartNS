using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace AnotherFolder.DeeperStructure.Folder_Dotted {
    
    [System.Serializable]
    public class qqwdw : PlayableAsset
    {
    	// Factory method that generates a playable based on this asset
    	public override Playable CreatePlayable(PlayableGraph graph, GameObject go) {
    		return Playable.Create(graph);
    	}
    }
}
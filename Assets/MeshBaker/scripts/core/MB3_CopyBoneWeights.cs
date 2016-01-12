using UnityEngine;
using System;
using System.Collections;

namespace DigitalOpus.MB.Core {
    public class MB3_CopyBoneWeights {
        public static void CopyBoneWeightsFromSeamMeshToOtherMeshes(float radius, Mesh seamMesh, Mesh[] targetMeshes) {
            //check radius is positive
            //Todo check that optimize is turned off on MeshImporter
            //Todo make sure mehses are assets
            //Todo check srcVerts and srcBW are not zero length
            if (seamMesh == null) {
                Debug.LogError(string.Format("The SeamMesh cannot be null"));
                return;
            }
			if (seamMesh.vertexCount == 0){
				Debug.LogError("The seam mesh has no vertices. Check that the Asset Importer for the seam mesh does not have 'Optimize Mesh' checked.");
				return;
			}
            Vector3[] srcVerts = seamMesh.vertices;
            BoneWeight[] srcBW = seamMesh.boneWeights;

			Debug.Log (string.Format("The seam mesh has {0} vertices.", seamMesh.vertices.Length));

            //validate
            bool failed = false;
            for (int meshIdx = 0; meshIdx < targetMeshes.Length; meshIdx++) {
                if (seamMesh == null) {
					Debug.LogError(string.Format("The SeamMesh cannot be null"));
                    failed = true;
                }
                if (targetMeshes[meshIdx] == null) {
						Debug.LogError(string.Format("Mesh {0} was null", meshIdx));
                    failed = true;
                }

                if (radius < 0f) {
                    Debug.LogError("radius must be zero or positive.");
                }
            }
            if (failed) {
                return;
            }
            for (int meshIdx = 0; meshIdx < targetMeshes.Length; meshIdx++) {
                Mesh tm = targetMeshes[meshIdx];
                Vector3[] otherVerts = tm.vertices;
                BoneWeight[] otherBWs = tm.boneWeights;

                //todo check mesh exists, bw exists and verts exists everything on the same rig
                int numMatches = 0;
                for (int i = 0; i < otherVerts.Length; i++) {
                    for (int j = 0; j < srcVerts.Length; j++) {
                        if (Vector3.Distance(otherVerts[i], srcVerts[j]) <= radius) {
                            numMatches++;
                            otherBWs[i] = srcBW[j];
                        }
                    }
                }
                if (numMatches > 0) {
                    targetMeshes[meshIdx].boneWeights = otherBWs;
                }
                Debug.Log(string.Format("Copied boneweights for {1} vertices in mesh {0} that matched positions in the seam mesh.", targetMeshes[meshIdx].name, numMatches));
            }
			Debug.Log (string.Format("The seam mesh has {0} vertices.", seamMesh.vertices.Length));
        }
    }
}

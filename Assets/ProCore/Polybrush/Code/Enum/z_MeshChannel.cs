using UnityEngine;

/**
 *	Mesh property map.
 */
public enum z_MeshChannel
{
	NULL,
	UV0,
	UV2,
	UV3,
	UV4,
	COLOR,
	TANGENT
};

public static class z_MeshChannelUtility
{
	public static z_MeshChannel StringToEnum(string str)
	{
		string upper = str.ToUpper();

		for(int i = 0; i < ((int) z_MeshChannel.TANGENT) + 1; i++)
			if( upper.Equals( ((z_MeshChannel)i).ToString() ) )
				return (z_MeshChannel)i;

		return z_MeshChannel.NULL;
	}
}

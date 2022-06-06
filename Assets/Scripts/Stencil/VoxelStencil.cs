using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxel
{
    public class VoxelStencil
    {
        [HideInInspector]
        public bool _fillType = false;

        public bool Apply(int x, int y, int z)
        {
            return _fillType;
        }
    } 
}
